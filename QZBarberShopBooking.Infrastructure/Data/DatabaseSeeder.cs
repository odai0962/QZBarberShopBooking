using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly string[] DefaultRoles = ["Admin", "Employee", "Customer"];

    // One row per page: which platforms it appears on, and which actions it defines permissions
    // for. An empty Actions list means the page is unrestricted — visible to every authenticated
    // role regardless of RolePermission grants.
    //
    // "Employees" also covers the mobile "Barbers" screen (Admin manage-barbers) — it's the same
    // CRUD feature (EmployeesController) regardless of client, so it gets Mobile added to its
    // platforms rather than a second page with a different code. Four pages below (Customers,
    // Gallery, ShopOverview, ShopSettings) have no backing endpoint yet — seeded anyway so the
    // mobile team can build navigation now and wire the screens up as those features land.
    private static readonly (string Code, string Name, string Icon, string Url, Platform[] Platforms, string[] Actions)[] PageSeeds =
    [
        ("Bookings", "Bookings", "calendar", "/bookings", [Platform.Web, Platform.Mobile], ["View", "Create", "Edit", "Delete"]),
        ("Employees", "Employees", "users", "/employees", [Platform.Web, Platform.Mobile], ["View", "Create", "Edit", "Delete"]),
        ("Services", "Services", "scissors", "/services", [Platform.Web, Platform.Mobile], ["View", "Create", "Edit", "Delete"]),
        ("Users", "Users", "user-cog", "/users", [Platform.Web], ["View", "Create", "Edit", "Delete"]),
        ("Profile", "Profile", "user", "/profile", [Platform.Web, Platform.Mobile], []),
        ("Home", "Home", "home", "/home", [Platform.Mobile], ["View"]),
        ("Dashboard", "Dashboard", "grid", "/dashboard", [Platform.Mobile], ["View"]),
        ("Appointments", "Appointments", "list", "/appointments", [Platform.Mobile], ["View", "Edit"]),
        ("Customers", "Customers", "users", "/customers", [Platform.Mobile], ["View"]),
        ("WorkingHours", "Working Hours", "clock", "/working-hours", [Platform.Mobile], ["View", "Edit"]),
        ("TimeOff", "Time Off", "calendar-off", "/time-off", [Platform.Mobile], ["View", "Create"]),
        ("Gallery", "Gallery", "image", "/gallery", [Platform.Mobile], ["View", "Edit"]),
        ("ShopOverview", "Shop Overview", "bar-chart", "/shop-overview", [Platform.Mobile], ["View"]),
        ("ShopSettings", "Shop Settings", "settings", "/shop-settings", [Platform.Mobile], ["View", "Edit"]),
        ("Settings", "Settings", "sliders", "/settings", [Platform.Mobile], []),
        ("Notifications", "Notifications", "bell", "/notifications", [Platform.Mobile], []),
    ];

    // Which {PageCode}.{Action} permissions each role is granted, derived from this app's actual
    // [Authorize(Roles=...)] attributes on BookingsController/EmployeesController/
    // ServicesController/UsersController. Fine-grained actions (confirm/cancel/complete/
    // toggle-status) fold into "Edit". Zero-permission pages (Profile/Settings/Notifications) need
    // no entries here — they're unrestricted. "Home" is Customer-only: Admin gets every Barber
    // page plus admin-exclusive ones, but not Customer's — those are mutually exclusive roles.
    private static readonly (string Role, string PageCode, string[] Actions)[] RoleGrants =
    [
        ("Admin", "Bookings", ["View", "Create", "Edit", "Delete"]),
        ("Admin", "Employees", ["View", "Create", "Edit", "Delete"]),
        ("Admin", "Services", ["View", "Create", "Edit", "Delete"]),
        ("Admin", "Users", ["View", "Create", "Edit", "Delete"]),
        ("Employee", "Bookings", ["View", "Edit"]),
        ("Employee", "Employees", ["View", "Edit"]),
        ("Employee", "Services", ["View"]),
        ("Customer", "Bookings", ["View", "Create", "Edit"]),
        ("Customer", "Services", ["View"]),
        ("Customer", "Home", ["View"]),
        ("Employee", "Dashboard", ["View"]),
        ("Admin", "Dashboard", ["View"]),
        ("Employee", "Appointments", ["View", "Edit"]),
        ("Admin", "Appointments", ["View", "Edit"]),
        ("Employee", "Customers", ["View"]),
        ("Admin", "Customers", ["View"]),
        ("Employee", "WorkingHours", ["View", "Edit"]),
        ("Admin", "WorkingHours", ["View", "Edit"]),
        ("Employee", "TimeOff", ["View", "Create"]),
        ("Admin", "TimeOff", ["View", "Create"]),
        ("Employee", "Gallery", ["View", "Edit"]),
        ("Admin", "Gallery", ["View", "Edit"]),
        ("Admin", "ShopOverview", ["View"]),
        ("Admin", "ShopSettings", ["View", "Edit"]),
    ];

    public static async Task SeedAsync(BarberShopDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(context, logger, cancellationToken);
        await SeedPagesAndPermissionsAsync(context, logger, cancellationToken);
    }

    private static async Task SeedRolesAsync(BarberShopDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var existingRoles = await context.UserRoles
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var missingRoles = DefaultRoles
            .Where(role => !existingRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingRoles.Count == 0)
        {
            logger.LogDebug("Database roles already seeded");
            return;
        }

        foreach (var roleName in missingRoles)
        {
            context.UserRoles.Add(new UserRole { Name = roleName });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded roles: {Roles}", string.Join(", ", missingRoles));
    }

    private static async Task SeedPagesAndPermissionsAsync(BarberShopDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.Pages.AnyAsync(cancellationToken))
        {
            logger.LogDebug("Pages/permissions already seeded");
            return;
        }

        var roleIdsByName = await context.UserRoles.ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);

        var permissionsByCode = new Dictionary<string, Permission>();

        foreach (var pageSeed in PageSeeds)
        {
            var page = new Page { Code = pageSeed.Code, PageName = pageSeed.Name, Icon = pageSeed.Icon, Url = pageSeed.Url };

            foreach (var platform in pageSeed.Platforms)
                page.PagePlatforms.Add(new PagePlatform { Platform = platform });

            foreach (var action in pageSeed.Actions)
            {
                var permissionCode = $"{pageSeed.Code}.{action}";
                var permission = new Permission { Code = permissionCode, Name = $"{pageSeed.Name} - {action}" };
                permissionsByCode[permissionCode] = permission;
                page.PagePermissions.Add(new PagePermission { Permission = permission });
            }

            context.Pages.Add(page);
        }

        foreach (var grant in RoleGrants)
        {
            if (!roleIdsByName.TryGetValue(grant.Role, out var roleId))
            {
                logger.LogWarning("Cannot seed permission grant: role {Role} not found", grant.Role);
                continue;
            }

            foreach (var action in grant.Actions)
            {
                var permissionCode = $"{grant.PageCode}.{action}";
                if (!permissionsByCode.TryGetValue(permissionCode, out var permission))
                {
                    logger.LogWarning("Cannot seed permission grant: permission {Permission} not found", permissionCode);
                    continue;
                }

                context.RolePermissions.Add(new RolePermission { RoleId = roleId, Permission = permission });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {PageCount} pages with permissions and platform assignments", PageSeeds.Length);
    }
}
