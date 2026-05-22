using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QZBarberShopBooking.Infrastructure.Data
{
    public class BarberShopDbContextFactory : IDesignTimeDbContextFactory<BarberShopDbContext>
    {
        public BarberShopDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                // Try to find it in the API project directory if running from Infrastructure directory
                var parentDir = Directory.GetParent(basePath)?.FullName;
                if (parentDir != null)
                {
                    var apiPath = Path.Combine(parentDir, "QZBarberShopBooking.API");
                    if (File.Exists(Path.Combine(apiPath, "appsettings.json")))
                    {
                        basePath = apiPath;
                    }
                }
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddUserSecrets("54390f90-e1df-43b1-a010-208e68444e4d")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BarberShopDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new BarberShopDbContext(optionsBuilder.Options);
        }
    }
}
