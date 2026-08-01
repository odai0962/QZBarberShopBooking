using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.DI;

public static class ServiceRegistrationExtensions
{
    // Repository<>/UnitOfWork registration lives in Infrastructure's own
    // AddInfrastructure() extension (called separately from Program.cs) — Service must not
    // reference Infrastructure at all, since it's the business-logic layer and should be
    // buildable/testable without pulling in EF Core.
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        var assembly = typeof(ServiceRegistrationExtensions).Assembly;
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
}
