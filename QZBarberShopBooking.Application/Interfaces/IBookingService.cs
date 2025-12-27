using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto> GetByIdAsync(int id);
        Task<IEnumerable<BookingDto>> GetAllAsync();
        Task<PaginatedResponse<BookingDto>> GetPagedAsync(PagedRequest request);
        Task<BookingDto> CreateAsync(CreateBookingDto createBookingDto, int customerId);
        Task<BookingDto> UpdateAsync(int id, UpdateBookingDto updateBookingDto);
        Task<bool> CancelAsync(int id, int userId);
        Task<bool> ConfirmAsync(int id, int employeeId);
        Task<bool> CompleteAsync(int id, int employeeId);
        Task<IEnumerable<TimeSlotDto>> GetAvailableTimeSlotsAsync(int employeeId, DateTime date);
        Task<IEnumerable<BookingDto>> GetCustomerBookingsAsync(int customerId);
        Task<IEnumerable<BookingDto>> GetEmployeeBookingsAsync(int employeeId, DateTime? date = null);
        Task<BookingStatsDto> GetStatsAsync(DateTime? startDate, DateTime? endDate);
    }
}
