using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QZBarberShopBooking.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure
{
    public static class DbContextConfigurator
    {
        public static BarberShopDbContext CreateDbContext(IConfiguration configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BarberShopDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new BarberShopDbContext(optionsBuilder.Options);
        }
    }
}
