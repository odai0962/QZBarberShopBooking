using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Users;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<UserDto>>.Success(users, "Users retrieved successfully"));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetPaged([FromQuery] PagedRequest request)
    {
        var result = await _userService.GetPagedAsync(request);
        return Ok(ApiResponse<PaginatedResponse<UserDto>>.Success(result, "Users retrieved successfully"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Success(user, "User retrieved successfully"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto createUserDto)
    {
        var user = await _userService.CreateAsync(createUserDto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            ApiResponse<UserDto>.Success(user, "User created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        var user = await _userService.UpdateAsync(id, updateUserDto);
        return Ok(ApiResponse<UserDto>.Success(user, "User updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse.Success("User deleted successfully"));
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id)
    {
        var isActive = await _userService.ToggleStatusAsync(id);
        return Ok(ApiResponse<bool>.Success(isActive,
            isActive ? "User activated successfully" : "User deactivated successfully"));
    }
}
