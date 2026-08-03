using QZBarberShopBooking.Application.DTO.Config;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IConfigService
    {
        Task<ModulesResponseDto> GetModulesAsync(int roleId, Platform platform);
    }
}
