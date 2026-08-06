using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.Infrastructure.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _longTerm;
        private readonly TimeSpan _shortTerm;
        private readonly ConcurrentDictionary<string, int> _versions = new();

        public MemoryCacheService(IMemoryCache cache, TimeSpan longTermExpiration, TimeSpan shortTermExpiration)
        {
            _cache = cache;
            _longTerm = longTermExpiration;
            _shortTerm = shortTermExpiration;
        }

        public Task<T> GetOrCreateLongTermAsync<T>(string key, Func<Task<T>> factory)
            => GetOrCreateAsync(key, _longTerm, factory);

        public Task<T> GetOrCreateShortTermAsync<T>(string key, Func<Task<T>> factory)
            => GetOrCreateAsync(key, _shortTerm, factory);

        private Task<T> GetOrCreateAsync<T>(string key, TimeSpan expiration, Func<Task<T>> factory)
            => _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = expiration;
                return factory();
            })!;

        public string BuildVersionedKey(string keyPrefix, params Type[] dependsOn)
        {
            var parts = dependsOn
                .Select(t => t.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(name => $"{name}:v{_versions.GetOrAdd(name, 0)}");

            return $"{keyPrefix}|{string.Join('|', parts)}";
        }

        public void BumpVersions(IEnumerable<Type> entityTypes)
        {
            foreach (var name in entityTypes.Select(t => t.Name).Distinct())
                _versions.AddOrUpdate(name, 1, (_, current) => current + 1);
        }
    }
}
