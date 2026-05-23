using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.API.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return Ok(ApiResponse<AuthResponseDto>.Success(result, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);

                // Don't reveal specific error details for security
                return Unauthorized(ApiResponse<AuthResponseDto>.Failure("Invalid email or password"));
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return Ok(ApiResponse<AuthResponseDto>.Success(result, "Registration successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
                return BadRequest(ApiResponse<AuthResponseDto>.Failure(ex.Message));
            }
        }

        [HttpPost("register-employee")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterEmployee([FromBody] RegisterEmployeeDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterEmployeeAsync(registerDto);
                return Ok(ApiResponse<AuthResponseDto>.Success(result, "Employee registration successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee registration failed for email: {Email}", registerDto.Email);
                return BadRequest(ApiResponse<AuthResponseDto>.Failure(ex.Message));
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return Ok(ApiResponse<AuthResponseDto>.Success(result, "Token refreshed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return Unauthorized(ApiResponse<AuthResponseDto>.Failure("Invalid or expired token"));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                await _authService.LogoutAsync(userId);
                return Ok(ApiResponse.Success("Logged out successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return BadRequest(ApiResponse.Failure("Logout failed"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                await _authService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
                return Ok(ApiResponse.Success("Password changed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed");
                return BadRequest(ApiResponse.Failure(ex.Message));
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await _authService.ResetPasswordAsync(resetPasswordDto.Email);
                return Ok(ApiResponse.Success("If the email exists, a reset link has been sent"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed for email: {Email}", resetPasswordDto.Email);
                return BadRequest(ApiResponse.Failure("Failed to process reset request"));
            }
        }

        [HttpPost("verify-reset-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> VerifyResetToken([FromBody] VerifyResetTokenDto verifyResetTokenDto)
        {
            try
            {
                await _authService.VerifyResetTokenAsync(
                    verifyResetTokenDto.Email,
                    verifyResetTokenDto.Token,
                    verifyResetTokenDto.NewPassword);

                return Ok(ApiResponse.Success("Password reset successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset token verification failed for email: {Email}", verifyResetTokenDto.Email);
                return BadRequest(ApiResponse.Failure("Invalid or expired reset token"));
            }
        }

    }
}
