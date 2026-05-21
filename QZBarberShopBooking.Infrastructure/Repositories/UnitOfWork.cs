using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BarberShopDbContext _context;
        private Dictionary<Type, object> _repositories;

        public UnitOfWork(BarberShopDbContext context)
        {
            _context = context;
            _repositories = new Dictionary<Type, object>();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
        public int SaveChanges() => _context.SaveChanges();

        public void Dispose()
        {
            _context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
