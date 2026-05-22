using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        int SaveChanges();
        Task ExecuteInTransactionAsync( Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    }
}
