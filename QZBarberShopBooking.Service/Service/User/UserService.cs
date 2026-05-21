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
using QZBarberShopBooking.Service.Service;

namespace QZBarberShopBooking.Service.Service.User
{
    public class UserService : IUserService, IScopedService
    {
        private readonly IRepository<QZBarberShopBooking.Domain.Entities.User> _userRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IRepository<QZBarberShopBooking.Domain.Entities.User> userRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserDto> GetCurrentUserProfile()
        {
            var currentUserId = UserContext.UserId;

            if (!currentUserId.HasValue)
                throw new UnauthorizedException("User not authenticated");

            var user = await _userRepository.GetAll().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId.Value);
            if (user == null)
                throw new NotFoundException("User", currentUserId.Value);

            return _mapper.Map<UserDto>(user);
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
            if (await _userRepository.AnyAsync(u => u.Email.ToLower() == createUserDto.Email.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]>
                { { "Email", new[] { "Email already registered" } } });

            if (await _userRepository.AnyAsync(u => u.Username.ToLower() == createUserDto.Username.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]>
                { { "Username", new[] { "Username already taken" } } });

            var user = _mapper.Map<QZBarberShopBooking.Domain.Entities.User>(createUserDto);
            user.PasswordHash = new PasswordService().HashPassword(createUserDto.Password);
            user.CreationDate = DateTime.UtcNow;
            user.IsActive = createUserDto.IsActive;

            await _userRepository.InsertAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdUser = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);

            return _mapper.Map<UserDto>(createdUser);
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

            return _mapper.Map<UserProfileDto>(updatedUser);
        }
    }
}
