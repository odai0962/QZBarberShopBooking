using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;
using BookingServiceUnderTest = QZBarberShopBooking.Service.Bookings.BookingService;

namespace QZBarberShopBooking.Tests.Service;

/// <summary>
/// Regression coverage for the IDOR fix (GET /api/Bookings/{id} previously had no ownership
/// check at all) and the related Admin-bypass bug found while fixing it (Cancel/Confirm/Complete
/// compared the acting Admin's own userId against CustomerId/EmployeeId and always failed).
/// </summary>
public class BookingServiceOwnershipTests
{
    private const int CustomerId = 1;
    private const int OwningEmployeeId = 2;
    private const int OtherEmployeeId = 3;
    private const int UnrelatedCustomerId = 4;
    private const int AdminId = 99;
    private const int BookingId = 1;

    private static async Task<BookingServiceUnderTest> BuildServiceAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seedContext = TestContextFactory.CreateContext(databaseName);

        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Customer" });
        seedContext.UserRoles.Add(new UserRole { Id = 2, Name = "Employee" });

        seedContext.Customers.Add(new Customer
        {
            Id = CustomerId, Username = "customer", Email = "customer@test.local",
            PasswordHash = "x", FirstName = "Cara", LastName = "Customer", PhoneNumber = "1", RoleId = 1
        });
        seedContext.Employees.Add(new Employee
        {
            Id = OwningEmployeeId, Username = "owning-emp", Email = "owning@test.local",
            PasswordHash = "x", FirstName = "Eli", LastName = "Employee", PhoneNumber = "2", RoleId = 2,
            IsAvailableForBooking = true
        });
        seedContext.Employees.Add(new Employee
        {
            Id = OtherEmployeeId, Username = "other-emp", Email = "other@test.local",
            PasswordHash = "x", FirstName = "Otto", LastName = "Other", PhoneNumber = "3", RoleId = 2,
            IsAvailableForBooking = true
        });
        seedContext.Customers.Add(new Customer
        {
            Id = UnrelatedCustomerId, Username = "unrelated", Email = "unrelated@test.local",
            PasswordHash = "x", FirstName = "Uma", LastName = "Unrelated", PhoneNumber = "4", RoleId = 1
        });

        seedContext.Bookings.Add(new Booking
        {
            Id = BookingId,
            BookingNumber = "ABC12345",
            BookingDate = DateTime.UtcNow.Date,
            StartTimeUtc = DateTime.UtcNow,
            EndTimeUtc = DateTime.UtcNow.AddMinutes(30),
            Status = BookingStatus.Confirmed,
            CustomerId = CustomerId,
            EmployeeId = OwningEmployeeId,
            SubTotal = 10,
            TaxAmount = 0,
            TotalAmount = 10
        });

        await seedContext.SaveChangesAsync();

        // A fresh context against the same database, standing in for the fresh scoped
        // DbContext a real HTTP request would get — nothing from seeding stays tracked here.
        var context = TestContextFactory.CreateContext(databaseName);
        IRepository<T> Repo<T>() where T : class => new Repository<T>(context);

        return new BookingServiceUnderTest(
            Repo<Booking>(),
            Repo<Customer>(),
            Repo<Employee>(),
            Repo<Domain.Entities.EmployeeService>(),
            Repo<EmployeeSchedule>(),
            Repo<EmployeeTimeOff>(),
            new NoOpNotificationService(),
            TestContextFactory.CreateMapper(),
            new UnitOfWork(context));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking_ForOwningCustomer()
    {
        var sut = await BuildServiceAsync();

        var result = await sut.GetByIdAsync(BookingId, CustomerId, isAdmin: false);

        Assert.Equal(BookingId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking_ForOwningEmployee()
    {
        var sut = await BuildServiceAsync();

        var result = await sut.GetByIdAsync(BookingId, OwningEmployeeId, isAdmin: false);

        Assert.Equal(BookingId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsBooking_ForAdmin_EvenWhenNotOwner()
    {
        var sut = await BuildServiceAsync();

        var result = await sut.GetByIdAsync(BookingId, AdminId, isAdmin: true);

        Assert.Equal(BookingId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsForbidden_ForUnrelatedNonAdminUser()
    {
        // This is the IDOR regression test: before the fix, this call succeeded and returned
        // another customer's booking (name, notes, pricing) to anyone with a valid token.
        var sut = await BuildServiceAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetByIdAsync(BookingId, UnrelatedCustomerId, isAdmin: false));
    }

    [Fact]
    public async Task CancelAsync_ThrowsForbidden_ForUnrelatedNonAdminUser()
    {
        var sut = await BuildServiceAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.CancelAsync(BookingId, UnrelatedCustomerId, isAdmin: false));
    }

    [Fact]
    public async Task CancelAsync_Succeeds_ForAdmin_EvenThoughAdminOwnsNothing()
    {
        // Regression test for the bug found alongside the IDOR: CancelAsync used to compare the
        // acting user's id against CustomerId/EmployeeId with no Admin bypass, so an Admin could
        // never cancel a booking that wasn't literally their own.
        var sut = await BuildServiceAsync();

        var result = await sut.CancelAsync(BookingId, AdminId, isAdmin: true);

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmAsync_Succeeds_ForAdmin_OnAnotherEmployeesBooking()
    {
        var sut = await BuildServiceAsync();

        var result = await sut.ConfirmAsync(BookingId, AdminId, isAdmin: true);

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmAsync_ThrowsForbidden_ForADifferentEmployee()
    {
        var sut = await BuildServiceAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.ConfirmAsync(BookingId, OtherEmployeeId, isAdmin: false));
    }
}
