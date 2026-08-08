using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Extensions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Helpers;

namespace QZBarberShopBooking.Service.Bookings;

public class BookingService : IBookingService, IScopedService
{
    private readonly IRepository<Domain.Entities.Booking> _bookingRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepository;
    private readonly IRepository<Domain.Entities.EmployeeService> _employeeServiceRepository;
    private readonly IRepository<EmployeeSchedule> _scheduleRepository;
    private readonly IRepository<EmployeeTimeOff> _timeOffRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    // Single source of truth for which BookingStatus transitions are legal. Every status-changing
    // method routes through EnsureValidTransition so this table is the only place the rules live.
    private static readonly Dictionary<BookingStatus, BookingStatus[]> ValidTransitions = new()
    {
        [BookingStatus.Pending] = [BookingStatus.Confirmed, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.CheckedIn, BookingStatus.Completed, BookingStatus.Cancelled],
        [BookingStatus.CheckedIn] = [BookingStatus.Completed, BookingStatus.Cancelled],
        [BookingStatus.Completed] = [],
        [BookingStatus.Cancelled] = []
    };

    public BookingService(
        IRepository<Domain.Entities.Booking> bookingRepository,
        IRepository<Customer> customerRepository,
        IRepository<Domain.Entities.Employee> employeeRepository,
        IRepository<Domain.Entities.EmployeeService> employeeServiceRepository,
        IRepository<EmployeeSchedule> scheduleRepository,
        IRepository<EmployeeTimeOff> timeOffRepository,
        INotificationService notificationService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
        _employeeRepository = employeeRepository;
        _employeeServiceRepository = employeeServiceRepository;
        _scheduleRepository = scheduleRepository;
        _timeOffRepository = timeOffRepository;
        _notificationService = notificationService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingDto> GetByIdAsync(int id, int requesterId, bool isAdmin)
    {
        var booking = await LoadBookingQuery().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (!isAdmin && booking.CustomerId != requesterId && booking.EmployeeId != requesterId)
            throw new ForbiddenException("You cannot view this booking.");

        return MapBooking(booking);
    }

    public async Task<IEnumerable<BookingDto>> GetAllAsync()
    {
        var bookings = await LoadBookingQuery()
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.StartTimeUtc)
            .ToListAsync();

        return bookings.Select(MapBooking);
    }

    public async Task<PaginatedResponse<BookingDto>> GetPagedAsync(PagedRequest request)
    {
        var query = LoadBookingQuery().Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(b =>
                b.BookingNumber.ToLower().Contains(term) ||
                b.Customer.FirstName.ToLower().Contains(term) ||
                b.Customer.LastName.ToLower().Contains(term) ||
                b.AssignedEmployee.FirstName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
            query = query.ApplyOrdering(request.SortBy, request.SortDescending);
        else
            query = query.OrderByDescending(b => b.StartTimeUtc);

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return PaginatedResponse<BookingDto>.Create(
            items.Select(MapBooking),
            request.PageNumber,
            request.PageSize,
            total);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingDto createBookingDto, int customerId)
    {
        BookingDto? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var customer = await _customerRepository.GetAll()
                .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, ct)
                ?? throw new NotFoundException("Customer", customerId);

            var booking = await BuildAndInsertBookingAsync(
                customer.Id,
                createBookingDto.EmployeeId,
                createBookingDto.RequestedStartUtc,
                createBookingDto.Notes,
                createBookingDto.Services,
                createBookingDto.DiscountPercentage,
                BookingStatus.Pending,
                BookingSource.Customer,
                createdBy: customerId,
                initiatedByUserId: null,
                ct);

            await _notificationService.NotifyEmployeeBookingCreatedAsync(booking, ct);

            var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id, ct);
            result = MapBooking(loaded);
        });

        return result!;
    }

    public async Task<BookingDto> CreateOnBehalfAsync(CreateBookingOnBehalfDto createBookingOnBehalfDto, int initiatorId)
    {
        BookingDto? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var customer = await _customerRepository.GetAll()
                .FirstOrDefaultAsync(c => c.Id == createBookingOnBehalfDto.CustomerId && !c.IsDeleted, ct)
                ?? throw new NotFoundException("Customer", createBookingOnBehalfDto.CustomerId);

            if (!customer.IsActive)
                throw new BusinessRuleException("This customer's account is not active.");

            var booking = await BuildAndInsertBookingAsync(
                customer.Id,
                createBookingOnBehalfDto.EmployeeId,
                createBookingOnBehalfDto.RequestedStartUtc,
                createBookingOnBehalfDto.Notes,
                createBookingOnBehalfDto.Services,
                createBookingOnBehalfDto.DiscountPercentage,
                BookingStatus.Pending,
                BookingSource.EmployeeOnBehalf,
                createdBy: initiatorId,
                initiatedByUserId: initiatorId,
                ct);

            await _notificationService.NotifyBookingApprovalRequestedAsync(booking, ct);

            var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id, ct);
            result = MapBooking(loaded);
        });

        return result!;
    }

    public async Task<BookingDto> UpdateAsync(int id, UpdateBookingDto updateBookingDto)
    {
        var booking = await _bookingRepository.GetAll()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        _mapper.Map(updateBookingDto, booking);
        booking.ModificationDate = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        // UpdateAsync is Admin-only at the controller (see BookingsController.Update), so the
        // ownership check in GetByIdAsync is intentionally bypassed here; requesterId is unused when isAdmin is true.
        return await GetByIdAsync(id, requesterId: 0, isAdmin: true);
    }

    public async Task<bool> CancelAsync(int id, int requesterId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetAll()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (!isAdmin && booking.CustomerId != requesterId && booking.EmployeeId != requesterId)
            throw new ForbiddenException("You cannot cancel this booking.");

        EnsureValidTransition(booking.Status, BookingStatus.Cancelled);

        var reason = isAdmin
            ? BookingCancellationReason.AdminCancelled
            : booking.CustomerId == requesterId
                ? BookingCancellationReason.CustomerCancelled
                : BookingCancellationReason.EmployeeCancelled;

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = requesterId;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        if (reason != BookingCancellationReason.CustomerCancelled)
            await _notificationService.NotifyBookingCancelledAsync(booking);

        return true;
    }

    public async Task<bool> ConfirmAsync(int id, int employeeId, bool isAdmin)
    {
        return await UpdateStatusForEmployeeAsync(id, employeeId, isAdmin, BookingStatus.Confirmed);
    }

    public async Task<bool> CheckInAsync(int id, int employeeId, bool isAdmin)
    {
        return await UpdateStatusForEmployeeAsync(id, employeeId, isAdmin, BookingStatus.CheckedIn);
    }

    public async Task<bool> CompleteAsync(int id, int employeeId, bool isAdmin)
    {
        return await UpdateStatusForEmployeeAsync(id, employeeId, isAdmin, BookingStatus.Completed);
    }

    public async Task<BookingDto> TransferAsync(int id, TransferBookingDto transferBookingDto, int requesterId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Services)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (!isAdmin && booking.EmployeeId != requesterId)
            throw new ForbiddenException("You cannot transfer this booking.");

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            throw new BusinessRuleException("Only a pending or confirmed booking can be transferred.");

        if (booking.StartTimeUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("Past bookings cannot be transferred.");

        if (transferBookingDto.EmployeeId == booking.EmployeeId)
            throw new BusinessRuleException("This booking is already assigned to that barber.");

        var targetEmployee = await _employeeRepository.GetAll()
            .FirstOrDefaultAsync(e => e.Id == transferBookingDto.EmployeeId && !e.IsDeleted && e.IsActive)
            ?? throw new NotFoundException("Employee", transferBookingDto.EmployeeId);

        if (targetEmployee.IsAvailableForBooking != true)
            throw new BusinessRuleException("This barber is not available for booking.");

        var requestedServices = booking.Services
            .Select(s => new CreateBookingServiceDto { ServiceId = s.ServiceId })
            .ToList();

        var lineItems = await ResolveLineItemsAsync(transferBookingDto.EmployeeId, requestedServices, default);
        var totalDuration = TimeSpan.FromTicks(lineItems.Sum(x => x.Duration.Ticks));
        var newEnd = booking.StartTimeUtc.Add(totalDuration);

        await EnsureNoOverlapAsync(transferBookingDto.EmployeeId, booking.StartTimeUtc, newEnd, booking.Id, default);

        // Preserve the original discount rate (not amount) since re-pricing per the new
        // employee can change SubTotal.
        var discountRate = booking.SubTotal > 0 ? (booking.DiscountAmount ?? 0m) / booking.SubTotal : 0m;
        var newSubTotal = lineItems.Sum(x => x.Price);
        var newDiscount = newSubTotal * discountRate;

        booking.EmployeeId = targetEmployee.Id;
        booking.EndTimeUtc = newEnd;
        booking.SubTotal = newSubTotal;
        booking.DiscountAmount = newDiscount;
        booking.TotalAmount = newSubTotal - newDiscount + booking.TaxAmount;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = requesterId;

        var cursor = booking.StartTimeUtc;
        var lineItemsQueue = new Queue<BookingLineItem>(lineItems);
        foreach (var line in booking.Services)
        {
            var item = lineItemsQueue.Dequeue();
            line.EmployeeId = targetEmployee.Id;
            line.Price = item.Price;
            line.StartTimeUtc = cursor;
            line.EndTimeUtc = cursor.Add(item.Duration);
            cursor = cursor.Add(item.Duration);
        }

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id);
        return MapBooking(loaded);
    }

    public async Task<BookingDto> RescheduleAsync(int id, RescheduleBookingDto rescheduleBookingDto, int customerId)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Services)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (booking.CustomerId != customerId)
            throw new ForbiddenException("You cannot reschedule this booking.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new BusinessRuleException("Only a confirmed booking can be rescheduled.");

        if (booking.RescheduleCount >= 1)
            throw new BusinessRuleException("This booking has already been rescheduled once.");

        if (DateTime.UtcNow >= booking.StartTimeUtc.AddHours(-2))
            throw new BusinessRuleException("A booking cannot be rescheduled within 2 hours of its start time.");

        var duration = booking.EndTimeUtc - booking.StartTimeUtc;
        var newStart = DateTime.SpecifyKind(rescheduleBookingDto.RequestedStartUtc, DateTimeKind.Utc);
        var newEnd = newStart.Add(duration);

        await EnsureNoOverlapAsync(booking.EmployeeId, newStart, newEnd, booking.Id, default);
        await EnsureNoActiveBookingSameDayAsync(booking.CustomerId, newStart.Date, booking.Id, default);

        var offset = newStart - booking.StartTimeUtc;
        foreach (var line in booking.Services)
        {
            line.StartTimeUtc = line.StartTimeUtc.Add(offset);
            line.EndTimeUtc = line.EndTimeUtc.Add(offset);
        }

        booking.BookingDate = newStart.Date;
        booking.StartTimeUtc = newStart;
        booking.EndTimeUtc = newEnd;
        booking.RescheduleCount++;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = customerId;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id);
        return MapBooking(loaded);
    }

    public async Task<BookingDto> CustomerApproveAsync(int id, int customerId)
    {
        var booking = await EnsureRespondableByCustomerAsync(id, customerId);

        EnsureValidTransition(booking.Status, BookingStatus.Confirmed);

        booking.Status = BookingStatus.Confirmed;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = customerId;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.NotifyBookingRespondedByCustomerAsync(booking, approved: true);

        var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id);
        return MapBooking(loaded);
    }

    public async Task<BookingDto> CustomerRejectAsync(int id, int customerId)
    {
        var booking = await EnsureRespondableByCustomerAsync(id, customerId);

        EnsureValidTransition(booking.Status, BookingStatus.Cancelled);

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = BookingCancellationReason.CustomerRejected;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = customerId;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.NotifyBookingRespondedByCustomerAsync(booking, approved: false);

        var loaded = await LoadBookingQuery().FirstAsync(b => b.Id == booking.Id);
        return MapBooking(loaded);
    }

    private async Task<Domain.Entities.Booking> EnsureRespondableByCustomerAsync(int id, int customerId)
    {
        var booking = await _bookingRepository.GetAll()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (booking.CustomerId != customerId)
            throw new ForbiddenException("You cannot respond to this booking.");

        if (booking.Source != BookingSource.EmployeeOnBehalf || booking.Status != BookingStatus.Pending)
            throw new BusinessRuleException("This booking does not require your approval.");

        return booking;
    }

    public async Task<IEnumerable<TimeSlotDto>> GetAvailableTimeSlotsAsync(
        int employeeId,
        DateTime date,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        if (durationMinutes <= 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "DurationMinutes", ["Duration must be greater than zero."] }
            });

        var employee = await _employeeRepository.GetAll()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted && e.IsActive, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var nextDay = day.AddDays(1);

        var schedules = await _scheduleRepository.GetAll()
            .Where(s => s.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var timeOffs = await _timeOffRepository.GetAll()
            .Where(t => t.EmployeeId == employeeId && t.IsApproved && t.EndDate > day && t.StartDate < nextDay)
            .ToListAsync(cancellationToken);

        var bookings = await _bookingRepository.GetAll()
            .Where(b => b.EmployeeId == employeeId && !b.IsDeleted && b.Status != BookingStatus.Cancelled
                && b.StartTimeUtc < nextDay && b.EndTimeUtc > day)
            .ToListAsync(cancellationToken);

        return BookingAvailabilityHelper.BuildAvailableSlots(
            day,
            durationMinutes,
            employee,
            schedules,
            timeOffs,
            bookings);
    }

    // 92 days (~3 months) comfortably covers one calendar month with headroom, well short of a
    // whole year of per-day availability queries.
    private const int MaxSummaryRangeDays = 92;

    public async Task<IEnumerable<DateAvailabilityDto>> GetAvailabilitySummaryAsync(
        int employeeId,
        DateOnly startDate,
        DateOnly endDate,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "EndDate", ["End date must not be before start date."] }
            });

        if (endDate.DayNumber - startDate.DayNumber > MaxSummaryRangeDays)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "EndDate", [$"Date range cannot exceed {MaxSummaryRangeDays} days."] }
            });

        var result = new List<DateAvailabilityDto>();
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var slots = await GetAvailableTimeSlotsAsync(
                employeeId, day.ToDateTime(TimeOnly.MinValue), durationMinutes, cancellationToken);
            result.Add(new DateAvailabilityDto { Date = day, HasAvailability = slots.Any() });
        }

        return result;
    }

    public async Task<IEnumerable<BookingDto>> GetCustomerBookingsAsync(int customerId)
    {
        var bookings = await LoadBookingQuery()
            .Where(b => b.CustomerId == customerId && !b.IsDeleted)
            .OrderByDescending(b => b.StartTimeUtc)
            .ToListAsync();

        return bookings.Select(MapBooking);
    }

    public async Task<IEnumerable<BookingDto>> GetEmployeeBookingsAsync(int employeeId, DateTime? date = null)
    {
        var query = LoadBookingQuery().Where(b => b.EmployeeId == employeeId && !b.IsDeleted);

        if (date.HasValue)
        {
            var day = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
            var next = day.AddDays(1);
            query = query.Where(b => b.StartTimeUtc < next && b.EndTimeUtc > day);
        }

        var bookings = await query.OrderBy(b => b.StartTimeUtc).ToListAsync();
        return bookings.Select(MapBooking);
    }

    public async Task<IEnumerable<BookingDto>> GetEmployeeNotificationsAsync(int employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var bookings = await LoadBookingQuery()
            .Where(b => b.EmployeeId == employeeId
                && !b.IsDeleted
                && b.Status == BookingStatus.Confirmed
                && b.StartTimeUtc >= today
                && b.StartTimeUtc < tomorrow.AddDays(7))
            .OrderBy(b => b.StartTimeUtc)
            .ToListAsync();

        return bookings.Select(MapBooking);
    }

    public async Task<BookingStatsDto> GetStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _bookingRepository.GetAll().Where(b => !b.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(b => b.BookingDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(b => b.BookingDate <= endDate.Value);

        var bookings = await query.ToListAsync();

        return new BookingStatsDto
        {
            TotalBookings = bookings.Count,
            PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
            ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed),
            CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
            CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
            TotalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount)
        };
    }

    private static void EnsureValidTransition(BookingStatus current, BookingStatus target)
    {
        if (!ValidTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
            throw new BusinessRuleException($"Cannot change booking status from {current} to {target}.");
    }

    private async Task<bool> UpdateStatusForEmployeeAsync(int id, int employeeId, bool isAdmin, BookingStatus status)
    {
        var booking = await _bookingRepository.GetAll()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
            ?? throw new NotFoundException("Booking", id);

        if (!isAdmin && booking.EmployeeId != employeeId)
            throw new ForbiddenException("You cannot update this booking.");

        EnsureValidTransition(booking.Status, status);

        booking.Status = status;
        booking.ModificationDate = DateTime.UtcNow;
        booking.ModifiedBy = employeeId;

        await _bookingRepository.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        if (status == BookingStatus.Confirmed)
            await _notificationService.NotifyBookingConfirmedAsync(booking);

        return true;
    }

    // Shared by CreateAsync/CreateOnBehalfAsync: resolves services, checks overlap and the
    // one-booking-per-day rule, builds the Booking + its BookingServiceLine rows, and inserts it.
    private async Task<Domain.Entities.Booking> BuildAndInsertBookingAsync(
        int customerId,
        int employeeId,
        DateTime requestedStartUtc,
        string? notes,
        List<CreateBookingServiceDto> services,
        int? discountPercentage,
        BookingStatus status,
        BookingSource source,
        int createdBy,
        int? initiatedByUserId,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetAll()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted && e.IsActive, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        if (employee.IsAvailableForBooking != true)
            throw new BusinessRuleException("This barber is not available for booking.");

        if (services.Count == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Services", ["Select at least one service."] }
            });

        var lineItems = await ResolveLineItemsAsync(employeeId, services, cancellationToken);
        var totalDuration = TimeSpan.FromTicks(lineItems.Sum(x => x.Duration.Ticks));
        var startUtc = DateTime.SpecifyKind(requestedStartUtc, DateTimeKind.Utc);
        var endUtc = startUtc.Add(totalDuration);

        await EnsureNoOverlapAsync(employeeId, startUtc, endUtc, null, cancellationToken);
        await EnsureNoActiveBookingSameDayAsync(customerId, startUtc.Date, null, cancellationToken);

        var subTotal = lineItems.Sum(x => x.Price);
        var discount = discountPercentage.HasValue
            ? subTotal * discountPercentage.Value / 100m
            : 0m;
        var tax = 0m;
        var total = subTotal - discount + tax;

        var booking = new Domain.Entities.Booking
        {
            BookingNumber = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            BookingDate = startUtc.Date,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            Status = status,
            Source = source,
            InitiatedByUserId = initiatedByUserId,
            Notes = notes,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            TotalAmount = total,
            CustomerId = customerId,
            EmployeeId = employee.Id,
            CreatedBy = createdBy,
            CreationDate = DateTime.UtcNow
        };

        var cursor = startUtc;
        foreach (var item in lineItems)
        {
            booking.Services.Add(new BookingServiceLine
            {
                ServiceId = item.ServiceId,
                EmployeeId = employee.Id,
                Price = item.Price,
                StartTimeUtc = cursor,
                EndTimeUtc = cursor.Add(item.Duration),
                CreatedBy = createdBy,
                CreationDate = DateTime.UtcNow
            });
            cursor = cursor.Add(item.Duration);
        }

        await _bookingRepository.InsertAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return booking;
    }

    private async Task EnsureNoOverlapAsync(
        int employeeId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        // This predicate is the same interval-overlap test as BookingAvailabilityHelper.Overlaps,
        // duplicated (not delegated) on purpose: it runs inside an EF Core expression tree that
        // gets translated to SQL, and EF cannot translate a call to an arbitrary C# method here.
        var hasOverlap = await _bookingRepository.GetAll()
            .AnyAsync(b =>
                b.EmployeeId == employeeId &&
                !b.IsDeleted &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTimeUtc < endUtc &&
                b.EndTimeUtc > startUtc &&
                (excludeBookingId == null || b.Id != excludeBookingId),
                cancellationToken);

        if (hasOverlap)
            throw new ConflictException("The selected time slot is no longer available.");
    }

    private async Task EnsureNoActiveBookingSameDayAsync(
        int customerId,
        DateTime date,
        int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        var day = date.Date;
        var hasBookingSameDay = await _bookingRepository.GetAll()
            .AnyAsync(b =>
                b.CustomerId == customerId &&
                !b.IsDeleted &&
                b.Status != BookingStatus.Cancelled &&
                b.BookingDate == day &&
                (excludeBookingId == null || b.Id != excludeBookingId),
                cancellationToken);

        if (hasBookingSameDay)
            throw new BusinessRuleException("You already have a booking on this day.");
    }

    private async Task<List<BookingLineItem>> ResolveLineItemsAsync(
        int employeeId,
        List<CreateBookingServiceDto> services,
        CancellationToken cancellationToken)
    {
        var items = new List<BookingLineItem>();

        foreach (var requested in services)
        {
            var link = await _employeeServiceRepository.GetAll()
                .Include(es => es.Service)
                .FirstOrDefaultAsync(es =>
                    es.EmployeeId == employeeId &&
                    es.ServiceId == requested.ServiceId &&
                    es.IsAvailable &&
                    es.Service.IsActive,
                    cancellationToken)
                ?? throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Services", [$"Service {requested.ServiceId} is not offered by this barber."] }
                });

            items.Add(new BookingLineItem(
                link.ServiceId,
                link.CustomDuration ?? link.Service.DefaultDuration,
                link.CustomPrice));
        }

        return items;
    }

    private IQueryable<Domain.Entities.Booking> LoadBookingQuery()
    {
        return _bookingRepository.GetAll()
            .Include(b => b.Customer).ThenInclude(c => c.Role)
            .Include(b => b.AssignedEmployee).ThenInclude(e => e.Role)
            .Include(b => b.Services).ThenInclude(s => s.Service);
    }

    private BookingDto MapBooking(Domain.Entities.Booking booking)
    {
        var dto = _mapper.Map<BookingDto>(booking);
        dto.CreatedAt = booking.CreationDate;
        return dto;
    }

    private sealed record BookingLineItem(int ServiceId, TimeSpan Duration, decimal Price);
}
