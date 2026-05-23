using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace QZBarberShopBooking.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly string[] DefaultRoles = ["Admin", "Employee", "Customer"];

    public static async Task SeedAsync(BarberShopDbContext context, ILogger logger, CancellationToken cancellationToken = default)
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
            context.UserRoles.Add(new Domain.Entities.UserRole { Name = roleName });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded roles: {Roles}", string.Join(", ", missingRoles));
    }
}
