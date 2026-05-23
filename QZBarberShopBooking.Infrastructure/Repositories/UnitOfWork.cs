using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Data;

namespace QZBarberShopBooking.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BarberShopDbContext _context;

    public UnitOfWork(BarberShopDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public int SaveChanges() => _context.SaveChanges();

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
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
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
