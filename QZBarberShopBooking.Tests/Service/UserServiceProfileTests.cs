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
/// Regression coverage for the UpdateProfileDto fix: DateOfBirth only exists on Customer, not
/// the base User, so UserService.UpdateProfileAsync has to route to the Customer-specific
/// AutoMapper map for it to actually persist instead of being silently dropped.
/// </summary>
public class UserServiceProfileTests
{
    [Fact]
    public async Task UpdateProfileAsync_PersistsDateOfBirth_ForACustomer()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seedContext = TestContextFactory.CreateContext(databaseName);
        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Customer" });
        seedContext.Customers.Add(new Customer
        {
            Id = 1, Username = "cara", Email = "cara@test.local", PasswordHash = "x",
            FirstName = "Cara", LastName = "Customer", PhoneNumber = "1", RoleId = 1
        });
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
            new PasswordService(),
            TestContextFactory.CreateMapper(),
            new UnitOfWork(context, cache),
            cache);

        var dob = new DateTime(1995, 4, 12, 0, 0, 0, DateTimeKind.Utc);

        await sut.UpdateProfileAsync(1, new UpdateProfileDto { DateOfBirth = dob });

        var verifyContext = TestContextFactory.CreateContext(databaseName);
        var persisted = await verifyContext.Customers.FindAsync(1);
        Assert.Equal(dob, persisted!.DateOfBirth);
    }
}
