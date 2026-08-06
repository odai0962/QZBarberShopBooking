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
    private const string BootstrapEmployeePassword = "ChangeMe!123";
    private const string BootstrapCustomerPassword = "ChangeMe!123";

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
            await EnsureBootstrapEmployeeAsync(dbContext, scope.ServiceProvider, logger);
            await EnsureBootstrapCustomerAsync(dbContext, scope.ServiceProvider, logger);
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

    private static async Task EnsureBootstrapEmployeeAsync(BarberShopDbContext dbContext, IServiceProvider services, ILogger logger)
    {
        if (await dbContext.Set<Employee>().AnyAsync())
            return;

        var employeeRole = await dbContext.UserRoles.FirstOrDefaultAsync(r => r.Name == "Employee");
        if (employeeRole is null)
        {
            logger.LogWarning("Cannot seed bootstrap employee: 'Employee' role not found.");
            return;
        }

        var passwordService = services.GetRequiredService<PasswordService>();

        dbContext.Set<Employee>().Add(new Employee
        {
            Username = "employee",
            Email = "employee@qzbarbershop.local",
            PasswordHash = passwordService.HashPassword(BootstrapEmployeePassword),
            FirstName = "Test",
            LastName = "Barber",
            PhoneNumber = "0000000001",
            IsActive = true,
            RoleId = employeeRole.Id,
            CreationDate = DateTime.UtcNow,
            Specialization = "General Haircut",
            HireDate = DateTime.UtcNow,
            IsAvailableForBooking = true
        });

        await dbContext.SaveChangesAsync();

        logger.LogWarning(
            "Seeded a development-only bootstrap employee — username: employee, password: {Password}.",
            BootstrapEmployeePassword);
    }

    private static async Task EnsureBootstrapCustomerAsync(BarberShopDbContext dbContext, IServiceProvider services, ILogger logger)
    {
        if (await dbContext.Set<Customer>().AnyAsync())
            return;

        var customerRole = await dbContext.UserRoles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole is null)
        {
            logger.LogWarning("Cannot seed bootstrap customer: 'Customer' role not found.");
            return;
        }

        var passwordService = services.GetRequiredService<PasswordService>();

        dbContext.Set<Customer>().Add(new Customer
        {
            Username = "customer",
            Email = "customer@qzbarbershop.local",
            PasswordHash = passwordService.HashPassword(BootstrapCustomerPassword),
            FirstName = "Test",
            LastName = "Customer",
            PhoneNumber = "0000000002",
            IsActive = true,
            RoleId = customerRole.Id,
            CreationDate = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        logger.LogWarning(
            "Seeded a development-only bootstrap customer — username: customer, password: {Password}.",
            BootstrapCustomerPassword);
    }
}
