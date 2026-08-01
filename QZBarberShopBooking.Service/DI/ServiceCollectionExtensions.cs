using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Validators.Auth;
using QZBarberShopBooking.Service.Mappings;
using System.Reflection;

namespace QZBarberShopBooking.Service.DI
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

            // Register validators from Application assembly
            var appAssembly = Assembly.GetAssembly(typeof(QZBarberShopBooking.Application.DTO.Auth.LoginDto));
            if (appAssembly != null)
                services.AddValidatorsFromAssembly(appAssembly);

            return services;
        }
    }
}
