using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Interface
{
    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        Task<T?> GetByIdAsync(int id);

        // Query operations
        IQueryable<T> GetAll(bool isTracking = false);
        IQueryable<T> GetAll(Expression<Func<T, bool>> predicate, bool isTracking = false);
        Task<List<T>> GetAllListAsync(bool isTracking = false);
        Task<List<T>> GetAllListAsync(Expression<Func<T, bool>> predicate, bool isTracking = false);

        IQueryable<T> GetWithIncludes(params Expression<Func<T, object>>[] includes);
        IQueryable<T> GetWithIncludes(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        void Insert(T entity);
        Task InsertAsync(T entity, CancellationToken cancellationToken);
        void InsertRange(IEnumerable<T> entities);
        Task InsertRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        void Delete(int id);
        Task DeleteAsync(int id);
        void Delete(T entity);
        Task DeleteAsync(T entity);
        void DeleteRange(IEnumerable<T> entities);
        Task DeleteRangeAsync(IEnumerable<T> entities);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    }
}
