using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.API.Extensions;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IRegistrationService _registrationService;
        private readonly IPasswordResetService _passwordResetService;

        public AuthController(
            IAuthenticationService authenticationService,
            IRegistrationService registrationService,
            IPasswordResetService passwordResetService)
        {
            _authenticationService = authenticationService;
            _registrationService = registrationService;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authenticationService.LoginAsync(loginDto);
            return Ok(ApiResponse<AuthResponseDto>.Success(result, "Login successful"));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _registrationService.RegisterAsync(registerDto);
            return Ok(ApiResponse<AuthResponseDto>.Success(result, "Registration successful"));
        }

        [HttpPost("register-employee")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterEmployee([FromBody] RegisterEmployeeDto registerDto)
        {
            var result = await _registrationService.RegisterEmployeeAsync(registerDto);
            return Ok(ApiResponse<AuthResponseDto>.Success(result, "Employee registration successful"));
        }

        [HttpPost("social-login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> SocialLogin([FromBody] SocialLoginDto socialLoginDto)
        {
            var result = await _authenticationService.SocialLoginAsync(socialLoginDto);
            return Ok(ApiResponse<AuthResponseDto>.Success(result, "Login successful"));
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto);
            return Ok(ApiResponse<AuthResponseDto>.Success(result, "Token refreshed successfully"));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            await _authenticationService.LogoutAsync(userId);
            return Ok(ApiResponse.Success("Logged out successfully"));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            await _passwordResetService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            return Ok(ApiResponse.Success("Password changed successfully"));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            await _passwordResetService.ResetPasswordAsync(resetPasswordDto.Email);
            return Ok(ApiResponse.Success("If the email exists, a reset link has been sent"));
        }

        [HttpPost("verify-reset-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> VerifyResetToken([FromBody] VerifyResetTokenDto verifyResetTokenDto)
        {
            await _passwordResetService.VerifyResetTokenAsync(
                verifyResetTokenDto.Email,
                verifyResetTokenDto.Token,
                verifyResetTokenDto.NewPassword);

            return Ok(ApiResponse.Success("Password reset successfully"));
        }
    }
}
