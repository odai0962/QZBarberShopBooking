using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        Task<T?> GetByIdAsync(int id);

        IQueryable<T> GetAll(bool isTracking = false);
        IQueryable<T> GetAll(Expression<Func<T, bool>> predicate, bool isTracking = false);
        Task<List<T>> GetAllListAsync(bool isTracking = false);
        Task<List<T>> GetAllListAsync(Expression<Func<T, bool>> predicate, bool isTracking = false);

        IQueryable<T> GetWithIncludes(params Expression<Func<T, object>>[] includes);
        IQueryable<T> GetWithIncludes(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        void Insert(T entity);
        Task InsertAsync(T entity, CancellationToken cancellationToken = default);
        void InsertRange(IEnumerable<T> entities);
        Task InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        void Update(T entity);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        void UpdateRange(IEnumerable<T> entities);

        void Delete(int id);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        void Delete(T entity);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        void DeleteRange(IEnumerable<T> entities);
        Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
