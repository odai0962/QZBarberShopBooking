namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IEmployeePhotoStorageService
    {
        Task<string> SavePhotoAsync(int employeeId, Stream content, string fileName, CancellationToken cancellationToken = default);
    }
}
