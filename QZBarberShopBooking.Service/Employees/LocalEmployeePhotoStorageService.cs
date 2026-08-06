using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Employees;

// Local-disk storage, served back out via app.UseStaticFiles() — no external storage account,
// no new NuGet package. Swappable later behind IEmployeePhotoStorageService (e.g. for Firebase
// Storage/S3) without touching EmployeeDto/PhotoUrl or any caller, since callers only ever see the
// returned URL string.
public class LocalEmployeePhotoStorageService : IEmployeePhotoStorageService, IScopedService
{
    private const string PhotoFolderName = "employee-photos";

    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalEmployeePhotoStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> SavePhotoAsync(int employeeId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var webRootPath = _environment.WebRootPath
            ?? throw new InvalidOperationException("WebRootPath is not configured.");

        var folder = Path.Combine(webRootPath, PhotoFolderName);
        Directory.CreateDirectory(folder);

        // Remove any previous photo for this employee first, regardless of its extension, so a
        // re-upload with a different file type doesn't leave an orphaned file behind.
        foreach (var existingFile in Directory.EnumerateFiles(folder, $"{employeeId}-*"))
            File.Delete(existingFile);

        var extension = Path.GetExtension(fileName);
        // A fresh, unique name on every upload — not just {employeeId}{extension} — so PhotoUrl
        // itself changes each time and no client-side HTTP cache keeps showing the old photo
        // under a URL it already cached.
        var storedFileName = $"{employeeId}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(folder, storedFileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No active HTTP request to build the photo URL from.");

        return $"{request.Scheme}://{request.Host}/{PhotoFolderName}/{storedFileName}";
    }
}
