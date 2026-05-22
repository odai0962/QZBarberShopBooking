using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Employees;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : BaseApiController
{
    private readonly IEmployeeService _employeeService;
    private readonly IServiceService _serviceService;

    public EmployeesController(IEmployeeService employeeService, IServiceService serviceService)
    {
        _employeeService = employeeService;
        _serviceService = serviceService;
    }

    [HttpGet("for-booking")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeDto>>>> GetForBooking()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(employees, "Barbers retrieved successfully"));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        return Ok(ApiResponse<EmployeeDto>.Success(employee, "Barber retrieved successfully"));
    }

    [HttpGet("{id:int}/services")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDto>>>> GetServices(int id)
    {
        var services = await _serviceService.GetServicesByEmployeeAsync(id);
        return Ok(ApiResponse<IEnumerable<ServiceDto>>.Success(services, "Services retrieved successfully"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeDto>>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var employees = await _employeeService.GetAllAsync(includeInactive);
        return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(employees, "Employees retrieved successfully"));
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EmployeeDto>>>> GetPaged([FromQuery] PagedRequest request)
    {
        var result = await _employeeService.GetPagedAsync(request);
        return Ok(ApiResponse<PaginatedResponse<EmployeeDto>>.Success(result, "Employees retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id },
            ApiResponse<EmployeeDto>.Success(employee, "Employee created successfully"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _employeeService.UpdateAsync(id, dto);
        return Ok(ApiResponse<EmployeeDto>.Success(employee, "Employee updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _employeeService.DeleteAsync(id);
        return Ok(ApiResponse.Success("Employee deleted successfully"));
    }

    [HttpPatch("{id:int}/availability")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleAvailability(int id)
    {
        var isAvailable = await _employeeService.ToggleAvailabilityAsync(id);
        return Ok(ApiResponse<bool>.Success(isAvailable, "Availability updated"));
    }

    [HttpPut("{id:int}/services")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> AssignServices(int id, [FromBody] AssignEmployeeServicesDto dto)
    {
        await _employeeService.AssignServicesAsync(id, dto);
        return Ok(ApiResponse.Success("Services assigned to employee"));
    }

    [HttpGet("{id:int}/schedule")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<ApiResponse<EmployeeScheduleDto>>> GetSchedule(int id)
    {
        var schedule = await _employeeService.GetScheduleAsync(id);
        return Ok(ApiResponse<EmployeeScheduleDto>.Success(schedule, "Schedule retrieved"));
    }

    [HttpPut("{id:int}/schedule")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<ApiResponse<EmployeeScheduleDto>>> UpdateSchedule(int id, [FromBody] UpdateScheduleDto dto)
    {
        var schedule = await _employeeService.UpdateScheduleAsync(id, dto);
        return Ok(ApiResponse<EmployeeScheduleDto>.Success(schedule, "Schedule updated"));
    }

    [HttpGet("{id:int}/time-offs")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeTimeOffDto>>>> GetTimeOffs(int id)
    {
        var timeOffs = await _employeeService.GetTimeOffsAsync(id);
        return Ok(ApiResponse<IEnumerable<EmployeeTimeOffDto>>.Success(timeOffs, "Time offs retrieved"));
    }

    [HttpPost("{id:int}/time-offs")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<ApiResponse<EmployeeTimeOffDto>>> CreateTimeOff(int id, [FromBody] CreateTimeOffDto dto)
    {
        var timeOff = await _employeeService.CreateTimeOffAsync(id, dto);
        return Ok(ApiResponse<EmployeeTimeOffDto>.Success(timeOff, "Time off created"));
    }

    [HttpGet("{id:int}/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmployeeStatsDto>>> GetStats(
        int id,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var stats = await _employeeService.GetStatsAsync(id, startDate, endDate);
        return Ok(ApiResponse<EmployeeStatsDto>.Success(stats, "Stats retrieved"));
    }
}
