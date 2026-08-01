using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto> GetByIdAsync(int id, int requesterId, bool isAdmin);
        Task<IEnumerable<BookingDto>> GetAllAsync();
        Task<PaginatedResponse<BookingDto>> GetPagedAsync(PagedRequest request);
        Task<BookingDto> CreateAsync(CreateBookingDto createBookingDto, int customerId);
        Task<BookingDto> UpdateAsync(int id, UpdateBookingDto updateBookingDto);
        Task<bool> CancelAsync(int id, int requesterId, bool isAdmin);
        Task<bool> ConfirmAsync(int id, int employeeId, bool isAdmin);
        Task<bool> CompleteAsync(int id, int employeeId, bool isAdmin);
        Task<IEnumerable<TimeSlotDto>> GetAvailableTimeSlotsAsync(int employeeId, DateTime date, int durationMinutes, CancellationToken cancellationToken = default);
        Task<IEnumerable<BookingDto>> GetCustomerBookingsAsync(int customerId);
        Task<IEnumerable<BookingDto>> GetEmployeeBookingsAsync(int employeeId, DateTime? date = null);
        Task<IEnumerable<BookingDto>> GetEmployeeNotificationsAsync(int employeeId);
        Task<BookingStatsDto> GetStatsAsync(DateTime? startDate, DateTime? endDate);
    }
}
