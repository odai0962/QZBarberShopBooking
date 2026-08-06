namespace QZBarberShopBooking.Application.Interfaces
{
    // Two intent-named tiers rather than a raw TimeSpan per call site — the actual minute values
    // are resolved once from configuration where MemoryCacheService is constructed, so business
    // logic only ever says which tier it means, never a number.
    public interface ICacheService
    {
        Task<T> GetOrCreateLongTermAsync<T>(string key, Func<Task<T>> factory);
        Task<T> GetOrCreateShortTermAsync<T>(string key, Func<Task<T>> factory);

        // Embeds each dependency's current version into the key, so a write that bumps one of
        // these types produces a different key for every future read — old entries are simply
        // never looked up again rather than explicitly removed. See MemoryCacheService for why.
        string BuildVersionedKey(string keyPrefix, params Type[] dependsOn);

        // Called once per successful UnitOfWork.SaveChanges(Async) with every entity type that
        // was Added/Modified/Deleted in that save.
        void BumpVersions(IEnumerable<Type> entityTypes);
    }
}
