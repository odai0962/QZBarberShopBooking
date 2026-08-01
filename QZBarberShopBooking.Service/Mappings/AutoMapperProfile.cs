using AutoMapper;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Customers;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
namespace QZBarberShopBooking.Service.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Auth Mappings
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.ToLower()));

            CreateMap<RegisterDto, Customer>()
                .IncludeBase<RegisterDto, User>()
                .ForMember(dest => dest.TotalVisits, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            CreateMap<RegisterEmployeeDto, Employee>()
                .IncludeBase<RegisterDto, User>()
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate ?? DateTime.UtcNow))
                .ForMember(dest => dest.IsAvailableForBooking, opt => opt.MapFrom(src => true));

            CreateMap<CreateEmployeeDto, Employee>()
                .IncludeBase<RegisterEmployeeDto, Employee>();

            CreateMap<CreateServiceDto, Domain.Entities.Service>();

            CreateMap<CreateTimeOffDto, EmployeeTimeOff>();

            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name));

            CreateMap<User, UserProfileDto>()
                .IncludeBase<User, UserDto>();

            CreateMap<Customer, CustomerDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.RecentBookings, opt => opt.MapFrom(src => src.Bookings.Take(5)));

            // Employee Mappings
            CreateMap<Employee, EmployeeDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.IsAvailableForBooking, opt => opt.MapFrom(src => src.IsAvailableForBooking ?? false))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(_ => (decimal?)null))
                .ForMember(dest => dest.TotalBookings, opt => opt.MapFrom(src => src.Bookings.Count))
                .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services.Where(es => es.IsAvailable).Select(es => es.Service)));

            CreateMap<EmployeeSchedule, EmployeeScheduleDayDto>();

            CreateMap<EmployeeTimeOff, EmployeeTimeOffDto>();

            // Booking Mappings
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.AssignedEmployee))
                .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services));

            CreateMap<BookingServiceLine, BookingServiceDto>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));

            // Service Mappings
            CreateMap<Domain.Entities.Service, ServiceDto>()
                .ForMember(dest => dest.TotalBookings,
                    opt => opt.MapFrom(src => src.BookingServices.Count))
                .ForMember(dest => dest.TotalRevenue,
                    opt => opt.MapFrom(src => src.BookingServices.Sum(bs => bs.Price)))
                .ForMember(dest => dest.AvailableEmployees,
                    opt => opt.MapFrom(src => src.EmployeeServices.Select(es => es.Employee)));

            CreateMap<EmployeeService, EmployeeServiceDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));

            CreateMap<CreateUserDto, Customer>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.ToLower()))
                .ForMember(dest => dest.TotalVisits, opt => opt.MapFrom(_ => 0));

            CreateMap<CreateUserDto, Employee>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.ToLower()))
                .ForMember(dest => dest.IsAvailableForBooking, opt => opt.MapFrom(_ => true));

            CreateMap<CreateUserDto, Admin>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.ToLower()));

            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateProfileDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // DateOfBirth only exists on Customer, not the base User, so a Customer's profile
            // update needs its own map — UserService.UpdateProfileAsync picks whichever of the
            // two applies based on the loaded entity's actual runtime type.
            CreateMap<UpdateProfileDto, Customer>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // UpdateCustomerDto/UpdateEmployeeDto do not actually inherit from UpdateUserDto in
            // C#, so .IncludeBase<UpdateUserDto, User>() here was invalid — AutoMapper throws
            // ArgumentOutOfRangeException building this profile the moment it's constructed,
            // which crashes the app at startup. Apply the same null-skip partial-update
            // condition directly instead, matching UpdateServiceDto/UpdateBookingDto below.
            CreateMap<UpdateCustomerDto, Customer>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateEmployeeDto, Employee>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateServiceDto, Domain.Entities.Service>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateBookingDto, Booking>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
