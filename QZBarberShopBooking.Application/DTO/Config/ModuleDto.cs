using QZBarberShopBooking.Application.DTO.Shared;

namespace QZBarberShopBooking.Application.DTO.Config
{
    public class ModuleDto
    {
        public required string Code { get; set; }
        public required LocalizedTextDto Name { get; set; }
        public string? Icon { get; set; }
        public required string Url { get; set; }
        public required Dictionary<string, bool> Permissions { get; set; }
    }

    public class ModulesResponseDto
    {
        public required List<ModuleDto> Modules { get; set; }
    }
}
