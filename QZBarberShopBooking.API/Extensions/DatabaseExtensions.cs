using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Service.Password;

namespace QZBarberShopBooking.API.Extensions;

public static class DatabaseExtensions
{
    // Development-only bootstrap credentials. UserService.CreateAsync requires an existing
    // Admin to create another Admin, so the very first one has to come from somewhere; this
    // seed only ever runs behind the IsDevelopment() guard below, never in Production.
    private const string BootstrapAdminPassword = "ChangeMe!123";

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BarberShopDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitialization");

        try
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Applied {Count} pending migration(s)", pendingMigrations.Count());
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database ensured created (no migrations configured)");
            }

            await DatabaseSeeder.SeedAsync(dbContext, logger);
            await EnsureBootstrapAdminAsync(dbContext, scope.ServiceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
        }
    }

    private static async Task EnsureBootstrapAdminAsync(BarberShopDbContext dbContext, IServiceProvider services, ILogger logger)
    {
        if (await dbContext.Set<Admin>().AnyAsync())
            return;

        var adminRole = await dbContext.UserRoles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            logger.LogWarning("Cannot seed bootstrap admin: 'Admin' role not found.");
            return;
        }

        var passwordService = services.GetRequiredService<PasswordService>();

        dbContext.Set<Admin>().Add(new Admin
        {
            Username = "admin",
            Email = "admin@qzbarbershop.local",
            PasswordHash = passwordService.HashPassword(BootstrapAdminPassword),
            FirstName = "System",
            LastName = "Admin",
            PhoneNumber = "0000000000",
            IsActive = true,
            RoleId = adminRole.Id,
            CreationDate = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        logger.LogWarning(
            "Seeded a development-only bootstrap admin — username: admin, password: {Password}. Change this immediately; use it only to create real Admin accounts via POST /api/Users.",
            BootstrapAdminPassword);
    }
}
