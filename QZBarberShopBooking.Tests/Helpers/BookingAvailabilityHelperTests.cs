using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Service.Helpers;
using Xunit;

namespace QZBarberShopBooking.Tests.Helpers;

public class BookingAvailabilityHelperTests
{
    private static readonly DateTime Monday = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc); // a Monday

    private static Employee MakeEmployee(bool availableForBooking = true, bool active = true) => new()
    {
        Id = 1,
        FirstName = "Sam",
        LastName = "Barber",
        IsAvailableForBooking = availableForBooking,
        IsActive = active
    };

    private static EmployeeSchedule WorkingDay(DayOfWeek day, int startHour, int endHour, TimeSpan? breakStart = null, TimeSpan? breakEnd = null) => new()
    {
        EmployeeId = 1,
        DayOfWeek = day,
        IsWorkingDay = true,
        StartTime = TimeSpan.FromHours(startHour),
        EndTime = TimeSpan.FromHours(endHour),
        BreakStartTime = breakStart,
        BreakEndTime = breakEnd
    };

    [Theory]
    [InlineData(10, 12, 11, 13, true)]   // overlapping
    [InlineData(10, 11, 11, 12, false)]  // back-to-back, not overlapping (half-open interval)
    [InlineData(10, 11, 8, 9, false)]    // entirely before
    [InlineData(9, 12, 10, 11, true)]    // fully contains
    public void Overlaps_DetectsOverlappingAndNonOverlappingIntervals(
        int aStartH, int aEndH, int bStartH, int bEndH, bool expected)
    {
        var aStart = Monday.AddHours(aStartH);
        var aEnd = Monday.AddHours(aEndH);
        var bStart = Monday.AddHours(bStartH);
        var bEnd = Monday.AddHours(bEndH);

        Assert.Equal(expected, BookingAvailabilityHelper.Overlaps(aStart, aEnd, bStart, bEnd));
    }

    [Fact]
    public void BuildAvailableSlots_ReturnsEmpty_WhenEmployeeNotAvailableForBooking()
    {
        var employee = MakeEmployee(availableForBooking: false);
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 17) };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, [], []);

        Assert.Empty(slots);
    }

    [Fact]
    public void BuildAvailableSlots_ReturnsEmpty_WhenNoWorkingScheduleForRequestedDay()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Tuesday, 9, 17) }; // Monday requested below

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, [], []);

        Assert.Empty(slots);
    }

    [Fact]
    public void BuildAvailableSlots_ExcludesSlotsInsideBreak()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 12, TimeSpan.FromHours(10), TimeSpan.FromHours(11)) };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, [], []).ToList();

        Assert.DoesNotContain(slots, s => s.StartTimeUtc >= Monday.AddHours(10) && s.StartTimeUtc < Monday.AddHours(11));
        Assert.Contains(slots, s => s.StartTimeUtc == Monday.AddHours(9));
    }

    [Fact]
    public void BuildAvailableSlots_ExcludesSlotsDuringApprovedTimeOff()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 12) };
        var timeOffs = new[]
        {
            new EmployeeTimeOff
            {
                EmployeeId = 1,
                IsApproved = true,
                StartDate = Monday.AddHours(9),
                EndDate = Monday.AddHours(11)
            }
        };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, timeOffs, []).ToList();

        Assert.All(slots, s => Assert.True(s.StartTimeUtc >= Monday.AddHours(11)));
    }

    [Fact]
    public void BuildAvailableSlots_IgnoresUnapprovedTimeOff()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 10) };
        var timeOffs = new[]
        {
            new EmployeeTimeOff
            {
                EmployeeId = 1,
                IsApproved = false,
                StartDate = Monday.AddHours(9),
                EndDate = Monday.AddHours(10)
            }
        };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, timeOffs, []);

        Assert.NotEmpty(slots);
    }

    [Fact]
    public void BuildAvailableSlots_ExcludesSlotsConflictingWithExistingBooking()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 12) };
        var bookings = new[]
        {
            new Booking
            {
                EmployeeId = 1,
                Status = BookingStatus.Confirmed,
                StartTimeUtc = Monday.AddHours(9),
                EndTimeUtc = Monday.AddHours(10)
            }
        };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, [], bookings).ToList();

        Assert.All(slots, s => Assert.True(s.StartTimeUtc >= Monday.AddHours(10)));
    }

    [Fact]
    public void BuildAvailableSlots_IgnoresCancelledBookings()
    {
        var employee = MakeEmployee();
        var schedules = new[] { WorkingDay(DayOfWeek.Monday, 9, 10) };
        var bookings = new[]
        {
            new Booking
            {
                EmployeeId = 1,
                Status = BookingStatus.Cancelled,
                StartTimeUtc = Monday.AddHours(9),
                EndTimeUtc = Monday.AddHours(10)
            }
        };

        var slots = BookingAvailabilityHelper.BuildAvailableSlots(
            Monday, 30, employee, schedules, [], bookings);

        Assert.NotEmpty(slots);
    }
}
