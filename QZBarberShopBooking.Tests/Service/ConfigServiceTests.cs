using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;
using ConfigServiceUnderTest = QZBarberShopBooking.Service.Config.ConfigService;

namespace QZBarberShopBooking.Tests.Service;

/// <summary>
/// Coverage for ConfigService's module-visibility rule: a page with no configured permissions is
/// unrestricted (visible to everyone, every action true); a page WITH configured permissions is
/// only returned if the caller's role holds at least one of them — holding none drops the page
/// from the response entirely rather than returning it with every action false, so the menu never
/// shows a dead item that would 403 on every click.
/// </summary>
public class ConfigServiceTests
{
    private const int RoleId = 1;
    private const int OtherRoleId = 2;

    private static void SeedPage(
        string databaseName,
        string code,
        Platform[] platforms,
        (string Action, int[] GrantedRoleIds)[] permissions)
    {
        var context = TestContextFactory.CreateContext(databaseName);

        var page = new Page { Code = code, PageNameEn = code, PageNameAr = code, Url = $"/{code.ToLowerInvariant()}" };
        foreach (var platform in platforms)
            page.PagePlatforms.Add(new PagePlatform { Platform = platform });

        foreach (var (action, grantedRoleIds) in permissions)
        {
            var permission = new Permission { Code = $"{code}.{action}", Name = $"{code} {action}" };
            page.PagePermissions.Add(new PagePermission { Permission = permission });

            foreach (var grantedRoleId in grantedRoleIds)
                context.RolePermissions.Add(new RolePermission { RoleId = grantedRoleId, Permission = permission });
        }

        context.Pages.Add(page);
        context.SaveChanges();
    }

    private static ConfigServiceUnderTest CreateSut(string databaseName)
    {
        var context = TestContextFactory.CreateContext(databaseName);
        return new ConfigServiceUnderTest(new Repository<Page>(context), TestContextFactory.CreateCacheService());
    }

    [Fact]
    public async Task GetModulesAsync_IncludesPage_WithAllPermissionsTrue_WhenPageHasNoConfiguredPermissions()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedPage(databaseName, "Profile", [Platform.Web], []);

        var result = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Web);

        var module = Assert.Single(result.Modules);
        Assert.Equal("Profile", module.Code);
        Assert.All(module.Permissions.Values, Assert.True);
    }

    [Fact]
    public async Task GetModulesAsync_ExcludesPage_WhenQueryingADifferentPlatform()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedPage(databaseName, "Bookings", [Platform.Web], []);

        var mobileResult = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Mobile);
        Assert.Empty(mobileResult.Modules);

        var webResult = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Web);
        Assert.Single(webResult.Modules);
    }

    [Fact]
    public async Task GetModulesAsync_PermissionsDict_ReflectsExactlyGrantedActions()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedPage(databaseName, "Bookings", [Platform.Web],
        [
            ("View", [RoleId]),
            ("Edit", [OtherRoleId]) // configured for the page, but granted to a different role
        ]);

        var result = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Web);

        var module = Assert.Single(result.Modules);
        Assert.True(module.Permissions["view"]);
        Assert.False(module.Permissions["create"]);
        Assert.False(module.Permissions["edit"]);
        Assert.False(module.Permissions["delete"]);
    }

    [Fact]
    public async Task GetModulesAsync_ExcludesPage_WhenRoleHoldsNoneOfItsConfiguredPermissions()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedPage(databaseName, "Users", [Platform.Web],
        [
            ("View", [OtherRoleId]),
            ("Edit", [OtherRoleId])
        ]);

        var result = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Web);

        Assert.Empty(result.Modules);
    }

    [Fact]
    public async Task GetModulesAsync_IncludesPage_WhenRoleHoldsAtLeastOnePermission()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedPage(databaseName, "Users", [Platform.Web],
        [
            ("View", [OtherRoleId]),
            ("Create", [OtherRoleId]),
            ("Edit", [OtherRoleId]),
            ("Delete", [RoleId])
        ]);

        var result = await CreateSut(databaseName).GetModulesAsync(RoleId, Platform.Web);

        var module = Assert.Single(result.Modules);
        Assert.False(module.Permissions["view"]);
        Assert.False(module.Permissions["create"]);
        Assert.False(module.Permissions["edit"]);
        Assert.True(module.Permissions["delete"]);
    }
}
