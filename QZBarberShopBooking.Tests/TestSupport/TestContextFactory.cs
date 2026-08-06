using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Caching;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Service.Mappings;

namespace QZBarberShopBooking.Tests.TestSupport;

/// <summary>
/// EF Core's InMemory provider gives real IAsyncQueryProvider support (unlike a plain
/// List&lt;T&gt;.AsQueryable()), so BookingService/UserService's FirstOrDefaultAsync/AnyAsync/
/// ToListAsync calls behave the same as against a real database. It does not support
/// transactions, so tests that go through IUnitOfWork.ExecuteInTransactionAsync are out of
/// scope here.
/// </summary>
internal static class TestContextFactory
{
    /// <summary>
    /// Creates a fresh, unconnected DbContext against a new in-memory database. Call this once
    /// per logical "request" in a test (one to seed, a separate one for the service under test)
    /// — reusing a single context for both leaves the seeded entities tracked, which makes
    /// Repository&lt;T&gt;.Update's Attach() collide with the still-tracked seed instance. A real
    /// HTTP request always gets its own scoped DbContext with nothing pre-tracked.
    /// </summary>
    public static BarberShopDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<BarberShopDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new BarberShopDbContext(options);
    }

    public static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    /// <summary>
    /// A real MemoryCache is free to construct, so there's no reason to fake ICacheService — each
    /// call site gets its own fresh instance, matching how each test already gets its own fresh
    /// databaseName/context (no cross-test cache pollution).
    /// </summary>
    public static ICacheService CreateCacheService()
        => new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(1440), TimeSpan.FromMinutes(15));
}

internal sealed class NoOpNotificationService : INotificationService
{
    public Task NotifyEmployeeBookingCreatedAsync(QZBarberShopBooking.Domain.Entities.Booking booking, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
