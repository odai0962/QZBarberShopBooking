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

            // Index مركب
            builder.HasIndex(bs => new { bs.BookingId, bs.ServiceId })
                .HasDatabaseName("IX_BookingService_BookingService");

            builder.HasIndex(bs => bs.TimeSlotId)
                .IsUnique() // One-to-One مع TimeSlot
                .HasDatabaseName("IX_BookingService_TimeSlot");

            builder.HasIndex(bs => bs.EmployeeId)
                .HasDatabaseName("IX_BookingService_Employee");

            // العلاقات
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

            builder.HasOne(bs => bs.TimeSlot)
                .WithOne(ts => ts.BookingService)
                .HasForeignKey<BookingService>(bs => bs.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // قيود البيانات
            builder.Property(bs => bs.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            // تسميات الجدول
            builder.ToTable("BookingServices", schema: "booking");
        }
    }
}