using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Infrastructure.Data;

namespace QZBarberShopBooking.API.Extensions;

public static class DatabaseExtensions
{
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
        }
    }
}
