using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.Id);

            // Index على BookingNumber للبحث السريع
            builder.HasIndex(b => b.BookingNumber)
                .IsUnique()
                .HasDatabaseName("IX_Booking_BookingNumber");

            // Index مركب للاستعلامات الشائعة
            builder.HasIndex(b => new { b.BookingDate, b.Status })
                .HasDatabaseName("IX_Booking_DateStatus");

            builder.HasIndex(b => new { b.CustomerId, b.BookingDate })
                .HasDatabaseName("IX_Booking_CustomerDate");

            // العلاقات
            builder.HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.AssignedEmployee)
                .WithMany()
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasMany(b => b.Services)
                .WithOne(bs => bs.Booking)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(b => b.BookingNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(b => b.Notes)
                .HasMaxLength(500);

            builder.Property(b => b.SubTotal)
                .HasPrecision(18, 2);

            builder.Property(b => b.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(b => b.TaxAmount)
                .HasPrecision(18, 2);

            builder.Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            builder.ToTable("Bookings", schema: "booking");
        }
    }
}
