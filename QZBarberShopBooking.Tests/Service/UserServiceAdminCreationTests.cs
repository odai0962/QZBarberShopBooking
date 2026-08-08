using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.Password;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;
using UserServiceUnderTest = QZBarberShopBooking.Service.Users.UserService;

namespace QZBarberShopBooking.Tests.Service;

/// <summary>
/// Regression coverage for the Admin-creation gap: UserService.CreateAsync's role switch used
/// to throw ValidationException for "Admin" (only "Customer"/"Employee" were handled), so no
/// code path in the product could ever create an Admin account.
/// </summary>
public class UserServiceAdminCreationTests
{
    [Fact]
    public async Task CreateAsync_CreatesAnAdminAccount_WhenRoleIsAdmin()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seedContext = TestContextFactory.CreateContext(databaseName);
        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Admin" });
        await seedContext.SaveChangesAsync();

        var context = TestContextFactory.CreateContext(databaseName);

        IRepository<T> Repo<T>() where T : class => new Repository<T>(context);
        var cache = TestContextFactory.CreateCacheService();

        var sut = new UserServiceUnderTest(
            Repo<Domain.Entities.User>(),
            Repo<UserRole>(),
            Repo<Customer>(),
            Repo<Employee>(),
            Repo<Admin>(),
            Repo<UserDeviceToken>(),
            new PasswordService(),
            TestContextFactory.CreateMapper(),
            new UnitOfWork(context, cache),
            cache);

        var dto = new CreateUserDto
        {
            Username = "newadmin",
            Email = "newadmin@test.local",
            Password = "Sup3r$ecret!",
            FirstName = "New",
            LastName = "Admin",
            PhoneNumber = "5551234",
            RoleId = 1,
            IsActive = true
        };

        var result = await sut.CreateAsync(dto);

        Assert.Equal("Admin", result.Role);
        Assert.Single(context.Set<Admin>());
    }
}
