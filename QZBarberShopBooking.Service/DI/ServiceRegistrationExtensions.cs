using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Service.Auth;
using System.Reflection;

namespace QZBarberShopBooking.Service.DI;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var assembly = typeof(AuthService).Assembly;
        var implementationTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && typeof(IScopedService).IsAssignableFrom(t));

        foreach (var implementationType in implementationTypes)
        {
            var serviceInterfaces = implementationType.GetInterfaces()
                .Where(i => i != typeof(IScopedService) && i.IsInterface)
                .ToList();

            if (serviceInterfaces.Count > 0)
            {
                foreach (var serviceInterface in serviceInterfaces)
                {
                    services.AddScoped(serviceInterface, implementationType);
                }
            }
            else
            {
                services.AddScoped(implementationType);
            }
        }

        return services;
    }

    [Obsolete("Use AddServiceLayer instead.")]
    public static IServiceCollection AddScopedServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly) => services.AddServiceLayer();
}
