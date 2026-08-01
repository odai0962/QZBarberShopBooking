using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Password;

namespace QZBarberShopBooking.Service.Auth
{
    public class RegistrationService : IRegistrationService, IScopedService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly PasswordService _passwordService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public RegistrationService(
            IRepository<User> userRepository,
            IRepository<UserRole> roleRepository,
            IRepository<Customer> customerRepository,
            IRepository<Employee> employeeRepository,
            PasswordService passwordService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository;
            _passwordService = passwordService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            if (await _userRepository.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]> { { "Email", new[] { "Email already registered" } } });

            if (await _userRepository.AnyAsync(u => u.Username.ToLower() == registerDto.Username.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]> { { "Username", new[] { "Username already taken" } } });

            var role = await _roleRepository.GetAll()
                .FirstOrDefaultAsync(r => r.Name == "Customer", cancellationToken)
                ?? throw new NotFoundException("Role", "Customer");

            var customer = _mapper.Map<Customer>(registerDto);
            customer.PasswordHash = _passwordService.HashPassword(registerDto.Password);
            customer.RoleId = role.Id;
            customer.IsActive = true;
            customer.CreationDate = DateTime.UtcNow;

            await _customerRepository.InsertAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await _tokenService.IssueTokensAsync(customer, cancellationToken);
        }

        public async Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto registerDto, CancellationToken cancellationToken = default)
        {
            var emailExists = await _userRepository.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());
            var usernameExists = await _userRepository.AnyAsync(u => u.Username.ToLower() == registerDto.Username.ToLower());

            if (emailExists)
                throw new ValidationException(new Dictionary<string, string[]> { { "Email", new[] { "Email already registered" } } });
            if (usernameExists)
                throw new ValidationException(new Dictionary<string, string[]> { { "Username", new[] { "Username already taken" } } });

            var role = await _roleRepository.GetAll()
                .FirstOrDefaultAsync(r => r.Name == "Employee", cancellationToken)
                ?? throw new NotFoundException("Role", "Employee");

            var employee = _mapper.Map<Employee>(registerDto);
            employee.PasswordHash = _passwordService.HashPassword(registerDto.Password);
            employee.RoleId = role.Id;
            employee.IsActive = true;
            employee.CreationDate = DateTime.UtcNow;
            employee.HireDate = DateTime.UtcNow;

            await _employeeRepository.InsertAsync(employee, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await _tokenService.IssueTokensAsync(employee, cancellationToken);
        }
    }
}
