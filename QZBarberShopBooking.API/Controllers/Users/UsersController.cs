using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.API.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<UserDto>>.Success(users, "Users retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all users");
                return StatusCode(500, ApiResponse<IEnumerable<UserDto>>.Failure("Failed to retrieve users"));
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                // استخدام الـ UserContext مباشرة
                if (!UserContext.HasUserId())
                    return Unauthorized(ApiResponse<UserDto>.Failure("User not authenticated", "Unauthorized"));

                var user = await _userService.GetCurrentUserProfile();
                return Ok(ApiResponse<UserDto>.Success(user, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user profile");
                return StatusCode(500, ApiResponse<UserDto>.Failure("An error occurred", "Server error"));
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetPaged([FromQuery] PagedRequest request)
        {
            try
            {
                var result = await _userService.GetPagedAsync(request);
                return Ok(ApiResponse<PaginatedResponse<UserDto>>.Success(result, "Users retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get paged users");
                return StatusCode(500, ApiResponse<PaginatedResponse<UserDto>>.Failure("Failed to retrieve users"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                return Ok(ApiResponse<UserDto>.Success(user, "User retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user with id: {Id}", id);
                return NotFound(ApiResponse<UserDto>.Failure($"User with id {id} not found"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var user = await _userService.CreateAsync(createUserDto);
                return CreatedAtAction(nameof(GetById), new { id = user.Id },
                    ApiResponse<UserDto>.Success(user, "User created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user");
                return BadRequest(ApiResponse<UserDto>.Failure(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userService.UpdateAsync(id, updateUserDto);
                return Ok(ApiResponse<UserDto>.Success(user, "User updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user with id: {Id}", id);
                return BadRequest(ApiResponse<UserDto>.Failure(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return Ok(ApiResponse.Success("User deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with id: {Id}", id);
                return BadRequest(ApiResponse.Failure(ex.Message));
            }
        }

        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id)
        {
            try
            {
                var isActive = await _userService.ToggleStatusAsync(id);
                return Ok(ApiResponse<bool>.Success(isActive,
                    isActive ? "User activated successfully" : "User deactivated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle status for user with id: {Id}", id);
                return BadRequest(ApiResponse<bool>.Failure(ex.Message));
            }
        }


        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
        {
            try
            {
                // Get user id from token
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(ApiResponse<UserProfileDto>.Failure("User not authenticated"));

                var profile = await _userService.UpdateProfileAsync(userId, updateProfileDto);
                return Ok(ApiResponse<UserProfileDto>.Success(profile, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user profile");
                return BadRequest(ApiResponse<UserProfileDto>.Failure(ex.Message));
            }
        }
    }
}
