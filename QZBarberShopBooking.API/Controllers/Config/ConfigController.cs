using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Config;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.API.Controllers.Config
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConfigController : BaseApiController
    {
        private readonly IConfigService _configService;

        public ConfigController(IConfigService configService)
        {
            _configService = configService;
        }

        [HttpGet("modules")]
        public async Task<ActionResult<ApiResponse<ModulesResponseDto>>> GetModules([FromQuery, BindRequired] Platform platform)
        {
            var roleId = UserContext.GetRoleIdOrThrow();
            var modules = await _configService.GetModulesAsync(roleId, platform);
            return Ok(ApiResponse<ModulesResponseDto>.Success(modules, "Modules retrieved successfully"));
        }
    }
}
