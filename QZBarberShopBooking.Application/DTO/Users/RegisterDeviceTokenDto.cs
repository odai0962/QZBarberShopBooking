namespace QZBarberShopBooking.Application.DTO.Users
{
    public class RegisterDeviceTokenDto
    {
        public required string DeviceToken { get; set; }
        public string? DeviceType { get; set; }
    }
}
