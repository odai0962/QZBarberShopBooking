using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.DI.DIType;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QZBarberShopBooking.Service.DI
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddScopedServicesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            var serviceTypes = assembly.GetTypes()
                .Where(t => typeof(IScopedService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var serviceType in serviceTypes)
            {
                var interfaceType = serviceType.GetInterfaces()
                    .FirstOrDefault(i => i == typeof(IScopedService));

                if (interfaceType != null)
                {
                    services.AddScoped(serviceType);
                }
            }

            return services;
        }
    }
}
