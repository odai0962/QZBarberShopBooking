using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
    {
        public void Configure(EntityTypeBuilder<BookingService> builder)
        {
            builder.HasKey(bs => bs.Id);

            builder.HasIndex(bs => new { bs.BookingId, bs.ServiceId })
                .HasDatabaseName("IX_BookingService_BookingService");

            builder.HasIndex(bs => bs.EmployeeId)
                .HasDatabaseName("IX_BookingService_Employee");

            builder.HasOne(bs => bs.Booking)
                .WithMany(b => b.Services)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bs => bs.Service)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bs => bs.Employee)
                .WithMany(e => e.BookingServices)
                .HasForeignKey(bs => bs.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(bs => bs.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.ToTable("BookingServices", schema: "booking");
        }
    }
}