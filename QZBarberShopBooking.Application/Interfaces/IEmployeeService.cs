using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.DTO.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeDto> GetByIdAsync(int id);
        Task<IEnumerable<EmployeeDto>> GetAllAsync(bool includeInactive = false);
        Task<PaginatedResponse<EmployeeDto>> GetPagedAsync(PagedRequest request);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto createEmployeeDto);
        Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto updateEmployeeDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleAvailabilityAsync(int id);
        Task<IEnumerable<EmployeeDto>> GetAvailableEmployeesAsync(DateTime date, TimeSpan time);
        Task<EmployeeScheduleDto> GetScheduleAsync(int employeeId);
        Task<EmployeeScheduleDto> UpdateScheduleAsync(int employeeId, UpdateScheduleDto updateScheduleDto);
        Task<IEnumerable<EmployeeTimeOffDto>> GetTimeOffsAsync(int employeeId);
        Task<EmployeeTimeOffDto> CreateTimeOffAsync(int employeeId, CreateTimeOffDto createTimeOffDto);
        Task<EmployeeStatsDto> GetStatsAsync(int employeeId, DateTime? startDate, DateTime? endDate);
        Task AssignServicesAsync(int employeeId, AssignEmployeeServicesDto dto);
        Task<EmployeeDto> UploadPhotoAsync(int employeeId, Stream content, string fileName, CancellationToken cancellationToken = default);
    }
}
