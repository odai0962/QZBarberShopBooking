using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Service.Helpers;

public static class BookingAvailabilityHelper
{
    private const int SlotStepMinutes = 15;

    public static IEnumerable<TimeSlotDto> BuildAvailableSlots(DateTime dateUtc, int durationMinutes,Employee employee,IEnumerable<EmployeeSchedule> schedules,IEnumerable<EmployeeTimeOff> timeOffs,IEnumerable<Booking> existingBookings)
    {
        if (employee.IsAvailableForBooking != true || !employee.IsActive)
            return [];

        var dayStart = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        var dayOfWeek = dayStart.DayOfWeek;
        var daySchedules = schedules
            .Where(s => s.DayOfWeek == dayOfWeek && s.IsWorkingDay)
            .ToList();

        if (daySchedules.Count == 0)
            return [];

        var duration = TimeSpan.FromMinutes(durationMinutes);
        var slots = new List<TimeSlotDto>();

        foreach (var schedule in daySchedules)
        {
            var windowStart = dayStart.Add(schedule.StartTime);
            var windowEnd = dayStart.Add(schedule.EndTime);

            for (var cursor = windowStart; cursor + duration <= windowEnd; cursor = cursor.AddMinutes(SlotStepMinutes))
            {
                var slotEnd = cursor + duration;

                if (IsInsideBreak(cursor, slotEnd, dayStart, schedule))
                    continue;

                if (IsInTimeOff(cursor, slotEnd, timeOffs))
                    continue;

                if (HasBookingConflict(cursor, slotEnd, existingBookings))
                    continue;

                slots.Add(new TimeSlotDto
                {
                    StartTimeUtc = cursor,
                    EndTimeUtc = slotEnd,
                    IsAvailable = true,
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}"
                });
            }
        }

        return slots;
    }

    private static bool IsInsideBreak(DateTime start, DateTime end, DateTime dayStart, EmployeeSchedule schedule)
    {
        if (!schedule.BreakStartTime.HasValue || !schedule.BreakEndTime.HasValue)
            return false;

        var breakStart = dayStart.Add(schedule.BreakStartTime.Value);
        var breakEnd = dayStart.Add(schedule.BreakEndTime.Value);
        return start < breakEnd && end > breakStart;
    }

    private static bool IsInTimeOff(DateTime start, DateTime end, IEnumerable<EmployeeTimeOff> timeOffs)
    {
        return timeOffs.Any(t =>
            t.IsApproved &&
            start < t.EndDate &&
            end > t.StartDate);
    }

    private static bool HasBookingConflict(DateTime start, DateTime end, IEnumerable<Booking> bookings)
    {
        return bookings.Any(b =>
            b.Status is not BookingStatus.Cancelled &&
            !b.IsDeleted &&
            start < b.EndTimeUtc &&
            end > b.StartTimeUtc);
    }
}
