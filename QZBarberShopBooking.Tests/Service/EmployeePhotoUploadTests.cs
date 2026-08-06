using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.Password;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;
using EmployeeServiceUnderTest = QZBarberShopBooking.Service.Employees.EmployeeService;
using LocalEmployeePhotoStorageServiceUnderTest = QZBarberShopBooking.Service.Employees.LocalEmployeePhotoStorageService;

namespace QZBarberShopBooking.Tests.Service;

public class EmployeePhotoUploadTests
{
    [Fact]
    public async Task UploadPhotoAsync_PersistsThePhotoUrl_ReturnedByTheStorageService()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seedContext = TestContextFactory.CreateContext(databaseName);
        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Employee" });
        seedContext.Employees.Add(new Employee
        {
            Id = 1, Username = "barber", Email = "barber@test.local", PasswordHash = "x",
            FirstName = "Bob", LastName = "Barber", PhoneNumber = "1", RoleId = 1
        });
        await seedContext.SaveChangesAsync();

        var context = TestContextFactory.CreateContext(databaseName);
        IRepository<T> Repo<T>() where T : class => new Repository<T>(context);
        var cache = TestContextFactory.CreateCacheService();

        var sut = new EmployeeServiceUnderTest(
            Repo<Employee>(),
            Repo<Domain.Entities.EmployeeService>(),
            Repo<EmployeeSchedule>(),
            Repo<EmployeeTimeOff>(),
            Repo<UserRole>(),
            Repo<Domain.Entities.Service>(),
            new PasswordService(),
            new FakePhotoStorageService("https://api.test.local/employee-photos/1-abc.jpg"),
            TestContextFactory.CreateMapper(),
            new UnitOfWork(context, cache),
            cache);

        var result = await sut.UploadPhotoAsync(1, Stream.Null, "photo.jpg");

        Assert.Equal("https://api.test.local/employee-photos/1-abc.jpg", result.PhotoUrl);

        var verifyContext = TestContextFactory.CreateContext(databaseName);
        var persisted = verifyContext.Set<Employee>().Single(e => e.Id == 1);
        Assert.Equal("https://api.test.local/employee-photos/1-abc.jpg", persisted.PhotoUrl);
    }

    [Fact]
    public async Task UploadPhotoAsync_Throws_ForAMissingEmployee()
    {
        var databaseName = Guid.NewGuid().ToString();
        var context = TestContextFactory.CreateContext(databaseName);
        IRepository<T> Repo<T>() where T : class => new Repository<T>(context);
        var cache = TestContextFactory.CreateCacheService();

        var sut = new EmployeeServiceUnderTest(
            Repo<Employee>(),
            Repo<Domain.Entities.EmployeeService>(),
            Repo<EmployeeSchedule>(),
            Repo<EmployeeTimeOff>(),
            Repo<UserRole>(),
            Repo<Domain.Entities.Service>(),
            new PasswordService(),
            new FakePhotoStorageService("unused"),
            TestContextFactory.CreateMapper(),
            new UnitOfWork(context, cache),
            cache);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UploadPhotoAsync(999, Stream.Null, "photo.jpg"));
    }

    [Fact]
    public async Task SavePhotoAsync_WritesTheFileToDisk_AndReturnsAUrlBuiltFromTheRequest()
    {
        var tempRoot = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var sut = new LocalEmployeePhotoStorageServiceUnderTest(
                new FakeWebHostEnvironment(tempRoot),
                new FakeHttpContextAccessor("https", "api.test.local"));

            var bytes = Encoding.UTF8.GetBytes("fake-image-bytes");
            using var content = new MemoryStream(bytes);

            var url = await sut.SavePhotoAsync(7, content, "photo.jpg");

            Assert.StartsWith("https://api.test.local/employee-photos/7-", url);
            Assert.EndsWith(".jpg", url);

            var savedFiles = Directory.GetFiles(Path.Combine(tempRoot, "employee-photos"), "7-*");
            var savedFile = Assert.Single(savedFiles);
            Assert.Equal(bytes, await File.ReadAllBytesAsync(savedFile));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SavePhotoAsync_RemovesThePreviousPhoto_AndReturnsADifferentUrl_OnReupload()
    {
        var tempRoot = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var sut = new LocalEmployeePhotoStorageServiceUnderTest(
                new FakeWebHostEnvironment(tempRoot),
                new FakeHttpContextAccessor("https", "api.test.local"));

            var firstUrl = await sut.SavePhotoAsync(7, new MemoryStream([1, 2, 3]), "photo.jpg");
            var secondUrl = await sut.SavePhotoAsync(7, new MemoryStream([4, 5, 6]), "photo.png");

            Assert.NotEqual(firstUrl, secondUrl);

            var remainingFiles = Directory.GetFiles(Path.Combine(tempRoot, "employee-photos"), "7-*");
            var remainingFile = Assert.Single(remainingFiles);
            Assert.EndsWith(".png", remainingFile); // the old .jpg was deleted, not left orphaned
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private sealed class FakePhotoStorageService(string urlToReturn) : IEmployeePhotoStorageService
    {
        public Task<string> SavePhotoAsync(int employeeId, Stream content, string fileName, CancellationToken cancellationToken = default)
            => Task.FromResult(urlToReturn);
    }

    private sealed class FakeWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Test";
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public FakeHttpContextAccessor(string scheme, string host)
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = scheme;
            context.Request.Host = new HostString(host);
            HttpContext = context;
        }

        public HttpContext? HttpContext { get; set; }
    }
}
