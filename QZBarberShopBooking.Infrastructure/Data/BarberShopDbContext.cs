using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Data
{
    public class BarberShopDbContext : DbContext
    {
        public BarberShopDbContext(DbContextOptions<BarberShopDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<EmployeeService> EmployeeServices { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingServiceLine> BookingServices { get; set; }
        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        public DbSet<EmployeeTimeOff> EmployeeTimeOffs { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<PagePermission> PagePermissions { get; set; }
        public DbSet<PagePlatform> PagePlatforms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BarberShopDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
