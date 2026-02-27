using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IServiceService
    {
        Task<ServiceDto> GetByIdAsync(int id);
        Task<IEnumerable<ServiceDto>> GetAllAsync(bool includeInactive = false);
        Task<PaginatedResponse<ServiceDto>> GetPagedAsync(PagedRequest request);
        Task<ServiceDto> CreateAsync(CreateServiceDto createServiceDto);
        Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto updateServiceDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
        Task<IEnumerable<ServiceDto>> GetByCategoryAsync(string category);
        Task<IEnumerable<ServiceDto>> GetServicesByEmployeeAsync(int employeeId);
    }
}
