using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class EmployeeServiceConfiguration : IEntityTypeConfiguration<EmployeeService>
    {
        public void Configure(EntityTypeBuilder<EmployeeService> builder)
        {
            builder.HasKey(es => es.Id);

            builder.HasAlternateKey(es => new { es.EmployeeId, es.ServiceId });

            builder.HasIndex(es => es.ServiceId)
                .HasDatabaseName("IX_EmployeeService_Service");

            builder.HasIndex(es => es.EmployeeId)
                .HasDatabaseName("IX_EmployeeService_Employee");

            builder.HasOne(es => es.Employee)
                .WithMany(e => e.Services)
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(es => es.Service)
                .WithMany(s => s.EmployeeServices)
                .HasForeignKey(es => es.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(es => es.CustomPrice)
                .HasPrecision(18, 2);

            builder.Property(es => es.CustomDuration)
                .HasColumnType("time");

            builder.Property(es => es.IsAvailable)
                .HasDefaultValue(true);

            builder.ToTable("EmployeeServices", schema: "service");
        }
    }
}
