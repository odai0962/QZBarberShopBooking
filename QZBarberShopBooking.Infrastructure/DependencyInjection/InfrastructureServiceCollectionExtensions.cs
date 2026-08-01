using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Repositories;

namespace QZBarberShopBooking.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
