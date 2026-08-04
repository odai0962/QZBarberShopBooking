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
/// Coverage for GetAvailabilitySummaryAsync — loops the existing, already-tested
/// GetAvailableTimeSlotsAsync once per day in a range and reports true/false per date, for the
/// mobile calendar's month coloring.
/// </summary>
public class BookingAvailabilitySummaryTests
{
    private const int EmployeeId = 1;
    private const int InactiveEmployeeId = 2;
    private const int CustomerId = 3;
    private const int DurationMinutes = 60;

    // Three consecutive calendar days, so each falls on a distinct weekday.
    private static readonly DateOnly WorkingDay = new(2026, 8, 3);
    private static readonly DateOnly DayOff = WorkingDay.AddDays(1);
    private static readonly DateOnly FullyBookedDay = WorkingDay.AddDays(2);

    private static async Task<BookingServiceUnderTest> BuildServiceAsync(string databaseName)
    {
        var seedContext = TestContextFactory.CreateContext(databaseName);

        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Employee" });
        seedContext.UserRoles.Add(new UserRole { Id = 2, Name = "Customer" });

        seedContext.Employees.Add(new Employee
        {
            Id = EmployeeId, Username = "barber", Email = "barber@test.local", PasswordHash = "x",
            FirstName = "Bob", LastName = "Barber", PhoneNumber = "1", RoleId = 1,
            IsActive = true, IsAvailableForBooking = true
        });
        seedContext.Employees.Add(new Employee
        {
            Id = InactiveEmployeeId, Username = "inactive", Email = "inactive@test.local", PasswordHash = "x",
            FirstName = "Ivy", LastName = "Inactive", PhoneNumber = "2", RoleId = 1,
            IsActive = false, IsAvailableForBooking = true
        });
        seedContext.Customers.Add(new Customer
        {
            Id = CustomerId, Username = "customer", Email = "customer@test.local", PasswordHash = "x",
            FirstName = "Cara", LastName = "Customer", PhoneNumber = "3", RoleId = 2
        });

        seedContext.EmployeeSchedules.Add(new EmployeeSchedule
        {
            EmployeeId = EmployeeId,
            DayOfWeek = WorkingDay.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsWorkingDay = true
        });
        seedContext.EmployeeSchedules.Add(new EmployeeSchedule
        {
            EmployeeId = EmployeeId,
            DayOfWeek = FullyBookedDay.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsWorkingDay = true
        });
        // DayOff's weekday intentionally gets no EmployeeSchedule row at all.

        var fullyBookedStart = FullyBookedDay.ToDateTime(new TimeOnly(9, 0));
        var fullyBookedEnd = FullyBookedDay.ToDateTime(new TimeOnly(17, 0));
        seedContext.Bookings.Add(new Booking
        {
            BookingNumber = "FULL0001",
            BookingDate = fullyBookedStart.Date,
            StartTimeUtc = fullyBookedStart,
            EndTimeUtc = fullyBookedEnd,
            Status = BookingStatus.Confirmed,
            CustomerId = CustomerId,
            EmployeeId = EmployeeId,
            SubTotal = 10,
            TaxAmount = 0,
            TotalAmount = 10
        });

        await seedContext.SaveChangesAsync();

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
    public async Task GetAvailabilitySummaryAsync_ReturnsOneEntryPerDay_WithCorrectAvailabilityPerDay()
    {
        var sut = await BuildServiceAsync(Guid.NewGuid().ToString());

        var result = (await sut.GetAvailabilitySummaryAsync(EmployeeId, WorkingDay, FullyBookedDay, DurationMinutes)).ToList();

        Assert.Equal(3, result.Count);

        Assert.True(result.Single(r => r.Date == WorkingDay).HasAvailability); // schedule, no bookings
        Assert.False(result.Single(r => r.Date == DayOff).HasAvailability); // no schedule for this weekday
        Assert.False(result.Single(r => r.Date == FullyBookedDay).HasAvailability); // schedule exists, but fully booked
    }

    [Fact]
    public async Task GetAvailabilitySummaryAsync_Throws_WhenStartDateIsAfterEndDate()
    {
        var sut = await BuildServiceAsync(Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.GetAvailabilitySummaryAsync(EmployeeId, WorkingDay.AddDays(1), WorkingDay, DurationMinutes));
    }

    [Fact]
    public async Task GetAvailabilitySummaryAsync_Throws_WhenRangeExceedsMaximum()
    {
        var sut = await BuildServiceAsync(Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.GetAvailabilitySummaryAsync(EmployeeId, WorkingDay, WorkingDay.AddDays(200), DurationMinutes));
    }

    [Fact]
    public async Task GetAvailabilitySummaryAsync_Throws_ForInactiveEmployee()
    {
        var sut = await BuildServiceAsync(Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetAvailabilitySummaryAsync(InactiveEmployeeId, WorkingDay, FullyBookedDay, DurationMinutes));
    }
}
