using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Services;

[Route("api/[controller]")]
[ApiController]
public class ServicesController : BaseApiController
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDto>>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var services = await _serviceService.GetAllAsync(includeInactive);
        return Ok(ApiResponse<IEnumerable<ServiceDto>>.Success(services, "Services retrieved successfully"));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetById(int id)
    {
        var service = await _serviceService.GetByIdAsync(id);
        return Ok(ApiResponse<ServiceDto>.Success(service, "Service retrieved successfully"));
    }

    [HttpGet("by-employee/{employeeId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDto>>>> GetByEmployee(int employeeId)
    {
        var services = await _serviceService.GetServicesByEmployeeAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<ServiceDto>>.Success(services, "Services retrieved successfully"));
    }

    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDto>>>> GetByCategory(string category)
    {
        var services = await _serviceService.GetByCategoryAsync(category);
        return Ok(ApiResponse<IEnumerable<ServiceDto>>.Success(services, "Services retrieved successfully"));
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ServiceDto>>>> GetPaged([FromQuery] PagedRequest request)
    {
        var result = await _serviceService.GetPagedAsync(request);
        return Ok(ApiResponse<PaginatedResponse<ServiceDto>>.Success(result, "Services retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Create([FromBody] CreateServiceDto dto)
    {
        var service = await _serviceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = service.Id },
            ApiResponse<ServiceDto>.Success(service, "Service created successfully"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Update(int id, [FromBody] UpdateServiceDto dto)
    {
        var service = await _serviceService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ServiceDto>.Success(service, "Service updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _serviceService.DeleteAsync(id);
        return Ok(ApiResponse.Success("Service deactivated successfully"));
    }

    [HttpPatch("{id:int}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id)
    {
        var isActive = await _serviceService.ToggleStatusAsync(id);
        return Ok(ApiResponse<bool>.Success(isActive, "Service status updated"));
    }
}
