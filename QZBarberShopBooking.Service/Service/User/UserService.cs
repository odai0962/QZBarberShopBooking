using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Extensions;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Service.User;

public class UserService : IUserService, IScopedService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<UserRole> _roleRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepository;
    private readonly PasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<UserRole> roleRepository,
        IRepository<Customer> customerRepository,
        IRepository<Domain.Entities.Employee> employeeRepository,
        PasswordService passwordService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
        _passwordService = passwordService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> GetCurrentUserProfile()
    {
        var currentUserId = UserContext.UserId
            ?? throw new UnauthorizedException("User not authenticated");

        return await GetByIdAsync(currentUserId);
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAll()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", id);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAll()
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<PaginatedResponse<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _userRepository.GetAll()
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.Username.ToLower().Contains(searchTerm) ||
                u.PhoneNumber.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(request.SortBy))
        {
            query = query.ApplyOrdering(request.SortBy, request.SortDescending);
        }
        else
        {
            query = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

        return PaginatedResponse<UserDto>.Create(userDtos, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.AnyAsync(u => u.Email.ToLower() == createUserDto.Email.ToLower(), cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            { { "Email", new[] { "Email already registered" } } });

        if (await _userRepository.AnyAsync(u => u.Username.ToLower() == createUserDto.Username.ToLower(), cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            { { "Username", new[] { "Username already taken" } } });

        var role = await _roleRepository.GetByIdAsync(createUserDto.RoleId)
            ?? throw new NotFoundException("Role", createUserDto.RoleId);

        var passwordHash = _passwordService.HashPassword(createUserDto.Password);
        Domain.Entities.User createdUser;

        switch (role.Name)
        {
            case "Customer":
                var customer = _mapper.Map<Customer>(createUserDto);
                customer.PasswordHash = passwordHash;
                customer.RoleId = role.Id;
                customer.CreationDate = DateTime.UtcNow;
                customer.IsActive = createUserDto.IsActive;
                await _customerRepository.InsertAsync(customer, cancellationToken);
                createdUser = customer;
                break;
            case "Employee":
                var employee = _mapper.Map<Domain.Entities.Employee>(createUserDto);
                employee.PasswordHash = passwordHash;
                employee.RoleId = role.Id;
                employee.CreationDate = DateTime.UtcNow;
                employee.HireDate = DateTime.UtcNow;
                employee.IsActive = createUserDto.IsActive;
                employee.IsAvailableForBooking = true;
                await _employeeRepository.InsertAsync(employee, cancellationToken);
                createdUser = employee;
                break;
            default:
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "RoleId", new[] { $"Role '{role.Name}' is not supported for user creation." } }
                });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(createdUser.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAll()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", id);

        _mapper.Map(updateUserDto, user);
        user.ModificationDate = DateTime.UtcNow;

        if (updateUserDto.RoleId.HasValue && updateUserDto.RoleId.Value != user.RoleId)
            user.RoleId = updateUserDto.RoleId.Value;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;
        user.IsActive = false;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        user.IsActive = !user.IsActive;
        user.ModificationDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.IsActive;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAll()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", userId);

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAll()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", userId);

        _mapper.Map(updateProfileDto, user);
        user.ModificationDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedUser = await _userRepository.GetAll()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return _mapper.Map<UserProfileDto>(updatedUser!);
    }
}
