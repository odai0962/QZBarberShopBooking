using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Caching;
using QZBarberShopBooking.Infrastructure.Repositories;

namespace QZBarberShopBooking.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var cachingSection = configuration.GetSection("Caching");
        var longTerm = TimeSpan.FromMinutes(cachingSection.GetValue("LongTermMinutes", 1440));
        var shortTerm = TimeSpan.FromMinutes(cachingSection.GetValue("ShortTermMinutes", 15));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService>(sp =>
            new MemoryCacheService(sp.GetRequiredService<IMemoryCache>(), longTerm, shortTerm));

        return services;
    }
}
