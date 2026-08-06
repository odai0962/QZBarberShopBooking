using Microsoft.Extensions.Caching.Memory;
using QZBarberShopBooking.Infrastructure.Caching;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;

namespace QZBarberShopBooking.Tests.Service;

file class EntityA { }
file class EntityB { }

public class MemoryCacheServiceTests
{
    private static MemoryCacheService CreateSut()
        => new(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

    [Fact]
    public async Task GetOrCreateShortTermAsync_DoesNotInvokeTheFactoryAgain_OnACacheHit()
    {
        var sut = CreateSut();
        var callCount = 0;

        Task<int> Factory()
        {
            callCount++;
            return Task.FromResult(42);
        }

        var first = await sut.GetOrCreateShortTermAsync("key", Factory);
        var second = await sut.GetOrCreateShortTermAsync("key", Factory);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void BuildVersionedKey_IsOrderIndependent_ForTheSameDependencySet()
    {
        var sut = CreateSut();

        var keyA = sut.BuildVersionedKey("prefix", typeof(EntityA), typeof(EntityB));
        var keyB = sut.BuildVersionedKey("prefix", typeof(EntityB), typeof(EntityA));

        Assert.Equal(keyA, keyB);
    }

    [Fact]
    public void BuildVersionedKey_ChangesAfterBumpingADependentType_ButNotForAnUnrelatedType()
    {
        var sut = CreateSut();
        var keyBefore = sut.BuildVersionedKey("prefix", typeof(EntityA));
        var unrelatedKeyBefore = sut.BuildVersionedKey("other", typeof(EntityB));

        sut.BumpVersions([typeof(EntityA)]);

        var keyAfter = sut.BuildVersionedKey("prefix", typeof(EntityA));
        var unrelatedKeyAfter = sut.BuildVersionedKey("other", typeof(EntityB));

        Assert.NotEqual(keyBefore, keyAfter);
        Assert.Equal(unrelatedKeyBefore, unrelatedKeyAfter);
    }

    [Fact]
    public async Task BumpVersions_MakesThePreviouslyCachedEntryUnreachable_ForcingAFreshFactoryCall()
    {
        var sut = CreateSut();
        var callCount = 0;

        Task<int> Factory()
        {
            callCount++;
            return Task.FromResult(callCount);
        }

        var keyBefore = sut.BuildVersionedKey("prefix", typeof(EntityA));
        var firstValue = await sut.GetOrCreateShortTermAsync(keyBefore, Factory);

        sut.BumpVersions([typeof(EntityA)]);

        var keyAfter = sut.BuildVersionedKey("prefix", typeof(EntityA));
        var secondValue = await sut.GetOrCreateShortTermAsync(keyAfter, Factory);

        Assert.NotEqual(keyBefore, keyAfter);
        Assert.Equal(1, firstValue);
        Assert.Equal(2, secondValue);
        Assert.Equal(2, callCount);
    }

    // The one integration point the whole invalidation design leans on: UnitOfWork.SaveChangesAsync
    // must actually bump versions for whatever entity types it just saved, not just for the ones
    // MemoryCacheService is told about directly.
    [Fact]
    public async Task UnitOfWorkSaveChangesAsync_BumpsTheVersion_ForEveryEntityTypeItSaves()
    {
        var cache = CreateSut();
        var context = TestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var unitOfWork = new UnitOfWork(context, cache);

        var keyBefore = cache.BuildVersionedKey("catalog:all", typeof(Domain.Entities.Service));

        var repository = new Repository<Domain.Entities.Service>(context);
        await repository.InsertAsync(new Domain.Entities.Service { Name = "Haircut", DefaultDuration = TimeSpan.FromMinutes(30), BasePrice = 20m });
        await unitOfWork.SaveChangesAsync();

        var keyAfter = cache.BuildVersionedKey("catalog:all", typeof(Domain.Entities.Service));

        Assert.NotEqual(keyBefore, keyAfter);
    }
}
