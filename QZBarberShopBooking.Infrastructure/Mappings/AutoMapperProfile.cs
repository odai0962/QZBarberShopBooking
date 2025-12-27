using AutoMapper;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Customers;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Users;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Mappings
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
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => CalculateRating(src)))
                .ForMember(dest => dest.TotalBookings, opt => opt.MapFrom(src => src.Bookings.Count))
                .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services.Select(es => es.Service)));

            CreateMap<EmployeeSchedule, EmployeeScheduleDayDto>();

            CreateMap<EmployeeTimeOff, EmployeeTimeOffDto>();

            // Booking Mappings
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.AssignedEmployee))
                .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services));

            CreateMap<BookingService, BookingServiceDto>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));

            CreateMap<TimeSlot, TimeSlotDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));

            // Service Mappings
            CreateMap<Service, ServiceDto>()
                .ForMember(dest => dest.TotalBookings,
                    opt => opt.MapFrom(src => src.BookingServices.Count))
                .ForMember(dest => dest.TotalRevenue,
                    opt => opt.MapFrom(src => src.BookingServices.Sum(bs => bs.Price)))
                .ForMember(dest => dest.AvailableEmployees,
                    opt => opt.MapFrom(src => src.EmployeeServices.Select(es => es.Employee)));

            CreateMap<EmployeeService, EmployeeServiceDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"));

            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateCustomerDto, Customer>()
                .IncludeBase<UpdateUserDto, User>();

            CreateMap<UpdateEmployeeDto, Employee>()
                .IncludeBase<UpdateUserDto, User>();

            CreateMap<UpdateServiceDto, Service>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateBookingDto, Booking>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }

        private static decimal CalculateRating(Employee employee)
        {
            // يمكن إضافة منطق حساب التقييم لاحقاً
            return 4.5m; // قيمة افتراضية
        }
    }
}
