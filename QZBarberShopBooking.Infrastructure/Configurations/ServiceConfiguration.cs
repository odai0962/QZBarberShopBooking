using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasIndex(s => s.Name)
                .IsUnique()
                .HasDatabaseName("IX_Service_Name");

            builder.HasIndex(s => s.Category)
                .HasDatabaseName("IX_Service_Category");

            builder.HasIndex(s => new { s.IsActive, s.Category })
                .HasDatabaseName("IX_Service_ActiveCategory");

            builder.HasMany(s => s.EmployeeServices)
                .WithOne(es => es.Service)
                .HasForeignKey(es => es.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.BookingServices)
                .WithOne(bs => bs.Service)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.DefaultDuration)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(s => s.BasePrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(s => s.Category)
                .HasMaxLength(50);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.ToTable("Services", schema: "service");
        }
    }
}
