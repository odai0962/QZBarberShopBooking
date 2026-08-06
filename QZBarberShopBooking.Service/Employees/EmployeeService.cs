using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Extensions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Password;

namespace QZBarberShopBooking.Service.Employees;

public class EmployeeService : IEmployeeService, IScopedService
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepository;
    private readonly IRepository<Domain.Entities.EmployeeService> _employeeServiceRepository;
    private readonly IRepository<EmployeeSchedule> _scheduleRepository;
    private readonly IRepository<EmployeeTimeOff> _timeOffRepository;
    private readonly IRepository<UserRole> _roleRepository;
    private readonly IRepository<Domain.Entities.Service> _serviceRepository;
    private readonly PasswordService _passwordService;
    private readonly IEmployeePhotoStorageService _photoStorageService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public EmployeeService(
        IRepository<Domain.Entities.Employee> employeeRepository,
        IRepository<Domain.Entities.EmployeeService> employeeServiceRepository,
        IRepository<EmployeeSchedule> scheduleRepository,
        IRepository<EmployeeTimeOff> timeOffRepository,
        IRepository<UserRole> roleRepository,
        IRepository<Domain.Entities.Service> serviceRepository,
        PasswordService passwordService,
        IEmployeePhotoStorageService photoStorageService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _employeeRepository = employeeRepository;
        _employeeServiceRepository = employeeServiceRepository;
        _scheduleRepository = scheduleRepository;
        _timeOffRepository = timeOffRepository;
        _roleRepository = roleRepository;
        _serviceRepository = serviceRepository;
        _passwordService = passwordService;
        _photoStorageService = photoStorageService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public Task<EmployeeDto> GetByIdAsync(int id)
    {
        var key = _cacheService.BuildVersionedKey($"employee:byid:{id}",
            typeof(Domain.Entities.Employee), typeof(Domain.Entities.EmployeeService), typeof(Domain.Entities.Service));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var employee = await _employeeRepository.GetAll()
                .Include(e => e.Role)
                .Include(e => e.Services).ThenInclude(es => es.Service)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
                ?? throw new NotFoundException("Employee", id);

            return _mapper.Map<EmployeeDto>(employee);
        });
    }

    public Task<IEnumerable<EmployeeDto>> GetAllAsync(bool includeInactive = false)
    {
        var key = _cacheService.BuildVersionedKey($"employee:all:{includeInactive}",
            typeof(Domain.Entities.Employee), typeof(Domain.Entities.EmployeeService), typeof(Domain.Entities.Service));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var query = _employeeRepository.GetAll()
                .Include(e => e.Role)
                .Include(e => e.Services).ThenInclude(es => es.Service)
                .Where(e => !e.IsDeleted);

            if (!includeInactive)
                query = query.Where(e => e.IsActive && e.IsAvailableForBooking == true);

            var employees = await query
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
        });
    }

    public async Task<PaginatedResponse<EmployeeDto>> GetPagedAsync(PagedRequest request)
    {
        var query = _employeeRepository.GetAll()
            .Include(e => e.Role)
            .Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
            query = query.ApplyOrdering(request.SortBy, request.SortDescending);
        else
            query = query.OrderBy(e => e.LastName);

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return PaginatedResponse<EmployeeDto>.Create(
            _mapper.Map<IEnumerable<EmployeeDto>>(items),
            request.PageNumber,
            request.PageSize,
            total);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto createEmployeeDto)
    {
        if (await _employeeRepository.GetAll().AnyAsync(e => e.Email.ToLower() == createEmployeeDto.Email.ToLower()))
            throw new ValidationException(new Dictionary<string, string[]>
            { { "Email", ["Email already registered."] } });

        if (await _employeeRepository.GetAll().AnyAsync(e => e.Username.ToLower() == createEmployeeDto.Username.ToLower()))
            throw new ValidationException(new Dictionary<string, string[]>
            { { "Username", ["Username already taken."] } });

        var role = await _cacheService.GetOrCreateLongTermAsync("role:name:Employee",
                () => _roleRepository.GetAll().FirstOrDefaultAsync(r => r.Name == "Employee"))
            ?? throw new NotFoundException("Role", "Employee");

        var employee = _mapper.Map<Domain.Entities.Employee>(createEmployeeDto);
        employee.PasswordHash = _passwordService.HashPassword(createEmployeeDto.Password);
        employee.RoleId = role.Id;
        employee.IsActive = true;
        employee.IsAvailableForBooking = createEmployeeDto.IsAvailableForBooking;
        employee.HireDate = createEmployeeDto.HireDate ?? DateTime.UtcNow;
        employee.CreationDate = DateTime.UtcNow;
        employee.CreatedBy = 0;

        await _employeeRepository.InsertAsync(employee);
        await SeedDefaultScheduleAsync(employee.Id);
        await AssignServicesInternalAsync(employee.Id, createEmployeeDto.ServiceIds);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(employee.Id);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        var employee = await _employeeRepository.GetAll()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
            ?? throw new NotFoundException("Employee", id);

        _mapper.Map(updateEmployeeDto, employee);
        employee.ModificationDate = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Employee", id);

        employee.IsDeleted = true;
        employee.IsActive = false;
        employee.IsAvailableForBooking = false;
        employee.DeletedDate = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleAvailabilityAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Employee", id);

        employee.IsAvailableForBooking = !(employee.IsAvailableForBooking ?? false);
        employee.ModificationDate = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return employee.IsAvailableForBooking ?? false;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAvailableEmployeesAsync(DateTime date, TimeSpan time)
    {
        var employees = await _employeeRepository.GetAll()
            .Include(e => e.Role)
            .Include(e => e.Schedules)
            .Where(e => !e.IsDeleted && e.IsActive && e.IsAvailableForBooking == true)
            .ToListAsync();

        var result = employees.Where(e =>
            e.Schedules.Any(s =>
                s.IsWorkingDay &&
                s.DayOfWeek == date.DayOfWeek &&
                s.StartTime <= time &&
                s.EndTime >= time.Add(TimeSpan.FromMinutes(30))));

        return _mapper.Map<IEnumerable<EmployeeDto>>(result);
    }

    public Task<EmployeeScheduleDto> GetScheduleAsync(int employeeId)
    {
        var key = _cacheService.BuildVersionedKey(
            $"employee:schedule:{employeeId}", typeof(EmployeeSchedule), typeof(EmployeeTimeOff));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            _ = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            var schedules = await _scheduleRepository.GetAll()
                .Where(s => s.EmployeeId == employeeId)
                .OrderBy(s => s.DayOfWeek)
                .ToListAsync();

            var timeOffs = await _timeOffRepository.GetAll()
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return new EmployeeScheduleDto
            {
                EmployeeId = employeeId,
                ScheduleDays = _mapper.Map<List<EmployeeScheduleDayDto>>(schedules),
                TimeOffs = _mapper.Map<List<EmployeeTimeOffDto>>(timeOffs)
            };
        });
    }

    public async Task<EmployeeScheduleDto> UpdateScheduleAsync(int employeeId, UpdateScheduleDto updateScheduleDto)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId)
            ?? throw new NotFoundException("Employee", employeeId);

        var existing = await _scheduleRepository.GetAll()
            .Where(s => s.EmployeeId == employeeId)
            .ToListAsync();

        if (existing.Count > 0)
            _scheduleRepository.DeleteRange(existing);

        foreach (var day in updateScheduleDto.ScheduleDays)
        {
            await _scheduleRepository.InsertAsync(new EmployeeSchedule
            {
                EmployeeId = employeeId,
                DayOfWeek = day.DayOfWeek,
                StartTime = day.StartTime,
                EndTime = day.EndTime,
                IsWorkingDay = day.IsWorkingDay,
                BreakStartTime = day.BreakStartTime,
                BreakEndTime = day.BreakEndTime
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetScheduleAsync(employeeId);
    }

    public Task<IEnumerable<EmployeeTimeOffDto>> GetTimeOffsAsync(int employeeId)
    {
        var key = _cacheService.BuildVersionedKey($"employee:timeoffs:{employeeId}", typeof(EmployeeTimeOff));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var timeOffs = await _timeOffRepository.GetAll()
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EmployeeTimeOffDto>>(timeOffs);
        });
    }

    public async Task<EmployeeTimeOffDto> CreateTimeOffAsync(int employeeId, CreateTimeOffDto createTimeOffDto)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId)
            ?? throw new NotFoundException("Employee", employeeId);

        var timeOff = _mapper.Map<EmployeeTimeOff>(createTimeOffDto);
        timeOff.EmployeeId = employeeId;
        timeOff.IsApproved = true;

        await _timeOffRepository.InsertAsync(timeOff);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<EmployeeTimeOffDto>(timeOff);
    }

    public async Task<EmployeeStatsDto> GetStatsAsync(int employeeId, DateTime? startDate, DateTime? endDate)
    {
        var employee = await _employeeRepository.GetAll()
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new NotFoundException("Employee", employeeId);

        var bookings = employee.Bookings.Where(b => !b.IsDeleted).AsEnumerable();
        if (startDate.HasValue)
            bookings = bookings.Where(b => b.BookingDate >= startDate.Value);
        if (endDate.HasValue)
            bookings = bookings.Where(b => b.BookingDate <= endDate.Value);

        var list = bookings.ToList();

        return new EmployeeStatsDto
        {
            TotalBookings = list.Count,
            CompletedBookings = list.Count(b => b.Status == Domain.Enums.BookingStatus.Completed),
            CancelledBookings = list.Count(b => b.Status == Domain.Enums.BookingStatus.Cancelled),
            TotalRevenue = list.Where(b => b.Status == Domain.Enums.BookingStatus.Completed).Sum(b => b.TotalAmount)
        };
    }

    public async Task AssignServicesAsync(int employeeId, AssignEmployeeServicesDto dto)
    {
        await AssignServicesInternalAsync(employeeId, dto.ServiceIds);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<EmployeeDto> UploadPhotoAsync(int employeeId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId)
            ?? throw new NotFoundException("Employee", employeeId);

        employee.PhotoUrl = await _photoStorageService.SavePhotoAsync(employeeId, content, fileName, cancellationToken);
        employee.ModificationDate = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId);
    }

    private async Task AssignServicesInternalAsync(int employeeId, IEnumerable<int> serviceIds)
    {
        var ids = serviceIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var services = await _serviceRepository.GetAll()
            .Where(s => ids.Contains(s.Id) && s.IsActive)
            .ToListAsync();

        if (services.Count != ids.Count)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "ServiceIds", ["One or more services were not found or are inactive."] }
            });

        var existing = await _employeeServiceRepository.GetAll()
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync();

        foreach (var link in existing)
            _employeeServiceRepository.Delete(link);

        foreach (var service in services)
        {
            await _employeeServiceRepository.InsertAsync(new Domain.Entities.EmployeeService
            {
                EmployeeId = employeeId,
                ServiceId = service.Id,
                CustomPrice = service.BasePrice,
                CustomDuration = service.DefaultDuration,
                IsAvailable = true
            });
        }
    }

    private async Task SeedDefaultScheduleAsync(int employeeId)
    {
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Saturday; day++)
        {
            await _scheduleRepository.InsertAsync(new EmployeeSchedule
            {
                EmployeeId = employeeId,
                DayOfWeek = day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                IsWorkingDay = true
            });
        }
    }
}
