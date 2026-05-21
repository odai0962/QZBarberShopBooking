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

            builder.HasIndex(ts => new { ts.EmployeeId, ts.StartTimeUtc })
                .IsUnique()
                .HasDatabaseName("IX_TimeSlot_EmployeeStartTimeUtc");

            builder.HasIndex(ts => ts.StartTimeUtc)
                .HasDatabaseName("IX_TimeSlot_StartTimeUtc");

            builder.HasIndex(ts => new { ts.StartTimeUtc, ts.EndTimeUtc })
                .HasDatabaseName("IX_TimeSlot_TimeRangeUtc");

            builder.HasOne(ts => ts.Employee)
                .WithMany(e => e.TimeSlots)
                .HasForeignKey(ts => ts.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(ts => ts.StartTimeUtc)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(ts => ts.EndTimeUtc)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.ToTable(tb => tb.HasCheckConstraint(
                "CK_TimeSlot_Time",
                "[EndTimeUtc] > [StartTimeUtc] AND DATEDIFF(minute, [StartTimeUtc], [EndTimeUtc]) <= 240"
            ));

            builder.ToTable("TimeSlots", schema: "schedule");
        }
    }
}
