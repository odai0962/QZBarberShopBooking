using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Extensions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Catalog;

public class CatalogService : IServiceService, IScopedService
{
    private readonly IRepository<Domain.Entities.Service> _serviceRepository;
    private readonly IRepository<Domain.Entities.EmployeeService> _employeeServiceRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public CatalogService(
        IRepository<Domain.Entities.Service> serviceRepository,
        IRepository<Domain.Entities.EmployeeService> employeeServiceRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _serviceRepository = serviceRepository;
        _employeeServiceRepository = employeeServiceRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public Task<ServiceDto> GetByIdAsync(int id)
    {
        var key = _cacheService.BuildVersionedKey($"catalog:byid:{id}",
            typeof(Domain.Entities.Service), typeof(Domain.Entities.EmployeeService), typeof(Domain.Entities.Employee));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var service = await _serviceRepository.GetAll()
                .Include(s => s.EmployeeServices)
                .ThenInclude(es => es.Employee)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("Service", id);

            return _mapper.Map<ServiceDto>(service);
        });
    }

    public Task<IEnumerable<ServiceDto>> GetAllAsync(bool includeInactive = false)
    {
        var key = _cacheService.BuildVersionedKey($"catalog:all:{includeInactive}", typeof(Domain.Entities.Service));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var query = _serviceRepository.GetAll();
            if (!includeInactive)
                query = query.Where(s => s.IsActive);

            var services = await query
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        });
    }

    public async Task<PaginatedResponse<ServiceDto>> GetPagedAsync(PagedRequest request)
    {
        var query = _serviceRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Category != null && s.Category.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
            query = query.ApplyOrdering(request.SortBy, request.SortDescending);
        else
            query = query.OrderBy(s => s.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return PaginatedResponse<ServiceDto>.Create(
            _mapper.Map<IEnumerable<ServiceDto>>(items),
            request.PageNumber,
            request.PageSize,
            total);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceDto createServiceDto)
    {
        if (await _serviceRepository.AnyAsync(s => s.Name.ToLower() == createServiceDto.Name.ToLower()))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Name", ["A service with this name already exists."] }
            });

        var service = _mapper.Map<Domain.Entities.Service>(createServiceDto);
        service.IsActive = true;

        await _serviceRepository.InsertAsync(service);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(service.Id);
    }

    public async Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto updateServiceDto)
    {
        var service = await _serviceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Service", id);

        _mapper.Map(updateServiceDto, service);
        await _serviceRepository.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var service = await _serviceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Service", id);

        service.IsActive = false;
        await _serviceRepository.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var service = await _serviceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Service", id);

        service.IsActive = !service.IsActive;
        await _serviceRepository.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();
        return service.IsActive;
    }

    public Task<IEnumerable<ServiceDto>> GetByCategoryAsync(string category)
    {
        var key = _cacheService.BuildVersionedKey(
            $"catalog:category:{category.ToLowerInvariant()}", typeof(Domain.Entities.Service));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var services = await _serviceRepository.GetAll()
                .Where(s => s.IsActive && s.Category != null && s.Category.ToLower() == category.ToLower())
                .OrderBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        });
    }

    public Task<IEnumerable<ServiceDto>> GetServicesByEmployeeAsync(int employeeId)
    {
        var key = _cacheService.BuildVersionedKey($"catalog:byemployee:{employeeId}",
            typeof(Domain.Entities.Service), typeof(Domain.Entities.EmployeeService));

        return _cacheService.GetOrCreateShortTermAsync(key, async () =>
        {
            var services = await _employeeServiceRepository.GetAll()
                .Include(es => es.Service)
                .Where(es => es.EmployeeId == employeeId && es.IsAvailable && es.Service.IsActive)
                .Select(es => es.Service)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        });
    }
}
