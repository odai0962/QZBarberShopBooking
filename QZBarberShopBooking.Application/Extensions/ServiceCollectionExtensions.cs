using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Mappings;
using QZBarberShopBooking.Application.Validators.Auth;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QZBarberShopBooking.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });

            var assembly = Assembly.GetExecutingAssembly();
            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
            services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();

            return services;
        }
    }
}
