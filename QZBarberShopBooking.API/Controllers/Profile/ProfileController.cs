using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Profile;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        var user = await _userService.GetCurrentUserProfile();
        return Ok(ApiResponse<UserDto>.Success(user, "Profile retrieved successfully"));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        var userId = UserContext.GetUserIdOrThrow();
        var profile = await _userService.UpdateProfileAsync(userId, updateProfileDto);
        return Ok(ApiResponse<UserProfileDto>.Success(profile, "Profile updated successfully"));
    }
}
