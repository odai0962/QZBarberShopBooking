using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Data;

namespace QZBarberShopBooking.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BarberShopDbContext _context;
    private readonly ICacheService _cacheService;

    public UnitOfWork(BarberShopDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    // The one chokepoint every write in the codebase funnels through, so cache invalidation is
    // hooked here rather than scattered across every service's write methods — it also catches
    // cross-service cases automatically (invalidation is keyed by entity type, not by which
    // service wrote it). Raw context.SaveChangesAsync() calls in DatabaseSeeder/DatabaseExtensions
    // bypass this, but those only ever run at startup before the app accepts its first request.
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var touchedTypes = GetTouchedEntityTypes();
        var result = await _context.SaveChangesAsync(cancellationToken);
        _cacheService.BumpVersions(touchedTypes);
        return result;
    }

    public int SaveChanges()
    {
        var touchedTypes = GetTouchedEntityTypes();
        var result = _context.SaveChanges();
        _cacheService.BumpVersions(touchedTypes);
        return result;
    }

    // Must be captured before SaveChanges — EF resets tracked entities' states afterward, so
    // Added/Modified/Deleted entries wouldn't be there to inspect post-save.
    private List<Type> GetTouchedEntityTypes()
        => _context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.GetType())
            .Distinct()
            .ToList();

    // A raw BeginTransactionAsync can't be combined with EnableRetryOnFailure (configured on
    // this DbContext in Program.cs) — EF throws rather than silently retry only part of a
    // transaction. CreateExecutionStrategy().ExecuteAsync makes the whole transaction (begin +
    // action + commit) the retryable unit instead, which is the supported pattern.
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
