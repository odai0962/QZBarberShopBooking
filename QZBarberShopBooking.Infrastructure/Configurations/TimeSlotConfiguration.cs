using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> builder)
        {
            builder.HasKey(ts => ts.Id);

            builder.HasIndex(ts => new { ts.EmployeeId, ts.StartTime })
                .IsUnique()
                .HasDatabaseName("IX_TimeSlot_EmployeeStartTime");

            builder.HasIndex(ts => ts.StartTime)
                .HasDatabaseName("IX_TimeSlot_StartTime");

            builder.HasIndex(ts => new { ts.StartTime, ts.EndTime })
                .HasDatabaseName("IX_TimeSlot_TimeRange");

            builder.HasOne(ts => ts.Employee)
                .WithMany(e => e.TimeSlots)
                .HasForeignKey(ts => ts.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ts => ts.BookingService)
                .WithOne(bs => bs.TimeSlot)
                .HasForeignKey<TimeSlot>(ts => ts.BookingServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Property(ts => ts.StartTime)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(ts => ts.EndTime)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.ToTable(tb => tb.HasCheckConstraint(
                "CK_TimeSlot_Time",
                "[EndTime] > [StartTime] AND DATEDIFF(minute, [StartTime], [EndTime]) <= 240"
            ));

            builder.ToTable("TimeSlots", schema: "schedule");
        }
    }
}
