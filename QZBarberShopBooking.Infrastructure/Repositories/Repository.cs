using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QZBarberShopBooking.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly BarberShopDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(BarberShopDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        #region Read Operations
        public virtual T GetById(int id) => _dbSet.Find(id);

        public virtual async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public virtual IQueryable<T> GetAll(bool isTracking = false)
            => isTracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();

        public virtual IQueryable<T> GetAll(Expression<Func<T, bool>> predicate, bool isTracking = false)
            => isTracking ? _dbSet.Where(predicate) : _dbSet.Where(predicate).AsNoTracking();

        public virtual async Task<List<T>> GetAllListAsync(bool isTracking = false)
            => await GetAll(isTracking).ToListAsync();

        public virtual async Task<List<T>> GetAllListAsync(Expression<Func<T, bool>> predicate, bool isTracking = false)
            => await GetAll(predicate, isTracking).ToListAsync();

        public virtual IQueryable<T> GetWithIncludes(params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.AsQueryable();
            return includes.Aggregate(query, (current, include) => current.Include(include));
        }

        public virtual IQueryable<T> GetWithIncludes(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.Where(predicate);
            return includes.Aggregate(query, (current, include) => current.Include(include));
        }
        #endregion

        #region Create Operations
        public virtual void Insert(T entity) => _dbSet.Add(entity);

        public virtual async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
            => await _dbSet.AddAsync(entity, cancellationToken);

        public virtual void InsertRange(IEnumerable<T> entities)
        {
            _dbSet.AddRange(entities);
            _context.SaveChanges();
        }

        public virtual async Task InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            => await _dbSet.AddRangeAsync(entities, cancellationToken);
        #endregion

        #region Update Operations
        public virtual void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }
        #endregion

        #region Delete Operations
        public virtual void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null) Delete(entity);
        }

        public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
                await DeleteAsync(entity, cancellationToken);
        }

        public virtual void Delete(T entity) => _dbSet.Remove(entity);

        public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            _context.SaveChanges();
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
        }
        #endregion

        #region Utility Operations
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await _dbSet.AnyAsync(predicate, cancellationToken);

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
            => predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
        #endregion
    }
}
