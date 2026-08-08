using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Bookings;

[Route("api/[controller]")]
[ApiController]
public class BookingsController : BaseApiController
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<TimeSlotDto>>>> GetAvailability(
        [FromQuery] int employeeId,
        [FromQuery] DateTime date,
        [FromQuery] int durationMinutes)
    {
        var slots = await _bookingService.GetAvailableTimeSlotsAsync(employeeId, date, durationMinutes);
        return Ok(ApiResponse<IEnumerable<TimeSlotDto>>.Success(slots, "Available slots retrieved"));
    }

    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create([FromBody] CreateBookingDto dto)
    {
        var customerId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.CreateAsync(dto, customerId);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id },
            ApiResponse<BookingDto>.Success(booking, "Booking created successfully"));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetMyBookings()
    {
        var customerId = UserContext.GetUserIdOrThrow();
        var bookings = await _bookingService.GetCustomerBookingsAsync(customerId);
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(bookings, "Bookings retrieved"));
    }

    [HttpGet("employee/my")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetEmployeeBookings([FromQuery] DateTime? date)
    {
        var employeeId = UserContext.GetUserIdOrThrow();
        var bookings = await _bookingService.GetEmployeeBookingsAsync(employeeId, date);
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(bookings, "Bookings retrieved"));
    }

    [HttpGet("employee/notifications")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetEmployeeNotifications()
    {
        var employeeId = UserContext.GetUserIdOrThrow();
        var bookings = await _bookingService.GetEmployeeNotificationsAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(bookings, "Notifications retrieved"));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(int id)
    {
        var requesterId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.GetByIdAsync(id, requesterId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking retrieved"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetAll()
    {
        var bookings = await _bookingService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Success(bookings, "Bookings retrieved"));
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<BookingDto>>>> GetPaged([FromQuery] PagedRequest request)
    {
        var result = await _bookingService.GetPagedAsync(request);
        return Ok(ApiResponse<PaginatedResponse<BookingDto>>.Success(result, "Bookings retrieved"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Update(int id, [FromBody] UpdateBookingDto dto)
    {
        var booking = await _bookingService.UpdateAsync(id, dto);
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking updated"));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Customer,Employee,Admin")]
    public async Task<ActionResult<ApiResponse>> Cancel(int id)
    {
        var userId = UserContext.GetUserIdOrThrow();
        await _bookingService.CancelAsync(id, userId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse.Success("Booking cancelled"));
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse>> Confirm(int id)
    {
        var employeeId = UserContext.GetUserIdOrThrow();
        await _bookingService.ConfirmAsync(id, employeeId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse.Success("Booking confirmed"));
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse>> Complete(int id)
    {
        var employeeId = UserContext.GetUserIdOrThrow();
        await _bookingService.CompleteAsync(id, employeeId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse.Success("Booking completed"));
    }

    [HttpPost("{id:int}/check-in")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse>> CheckIn(int id)
    {
        var employeeId = UserContext.GetUserIdOrThrow();
        await _bookingService.CheckInAsync(id, employeeId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse.Success("Customer checked in"));
    }

    [HttpPost("{id:int}/transfer")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Transfer(int id, [FromBody] TransferBookingDto dto)
    {
        var requesterId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.TransferAsync(id, dto, requesterId, UserContext.IsInRole("Admin"));
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking transferred"));
    }

    [HttpPost("{id:int}/reschedule")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Reschedule(int id, [FromBody] RescheduleBookingDto dto)
    {
        var customerId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.RescheduleAsync(id, dto, customerId);
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking rescheduled"));
    }

    [HttpPost("employee-initiated")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CreateOnBehalf([FromBody] CreateBookingOnBehalfDto dto)
    {
        var initiatorId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.CreateOnBehalfAsync(dto, initiatorId);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id },
            ApiResponse<BookingDto>.Success(booking, "Booking created, awaiting customer approval"));
    }

    [HttpPost("{id:int}/customer-approve")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CustomerApprove(int id)
    {
        var customerId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.CustomerApproveAsync(id, customerId);
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking approved"));
    }

    [HttpPost("{id:int}/customer-reject")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CustomerReject(int id)
    {
        var customerId = UserContext.GetUserIdOrThrow();
        var booking = await _bookingService.CustomerRejectAsync(id, customerId);
        return Ok(ApiResponse<BookingDto>.Success(booking, "Booking rejected"));
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BookingStatsDto>>> GetStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var stats = await _bookingService.GetStatsAsync(startDate, endDate);
        return Ok(ApiResponse<BookingStatsDto>.Success(stats, "Stats retrieved"));
    }
}
