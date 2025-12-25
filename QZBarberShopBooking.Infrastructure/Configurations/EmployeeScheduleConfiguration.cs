using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class EmployeeScheduleConfiguration : IEntityTypeConfiguration<EmployeeSchedule>
    {
  
            public void Configure(EntityTypeBuilder<EmployeeSchedule> builder)
            {
                builder.HasKey(es => es.Id);

                builder.HasIndex(es => new { es.EmployeeId, es.DayOfWeek })
                    .IsUnique()
                    .HasDatabaseName("IX_EmployeeSchedule_EmployeeDay");

                builder.HasOne(es => es.Employee)
                    .WithMany(e => e.Schedules)
                    .HasForeignKey(es => es.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Property(es => es.DayOfWeek)
                    .HasConversion<int>();

                builder.Property(es => es.StartTime)
                    .HasColumnType("time");

                builder.Property(es => es.EndTime)
                    .HasColumnType("time");

                builder.Property(es => es.BreakStartTime)
                    .HasColumnType("time");

                builder.Property(es => es.BreakEndTime)
                    .HasColumnType("time");

                builder.ToTable(tb => tb.HasCheckConstraint(
                    "CK_EmployeeSchedule_Time",
                    "[EndTime] > [StartTime] AND ([BreakStartTime] IS NULL OR [BreakEndTime] > [BreakStartTime])"
                ));

                builder.ToTable("EmployeeSchedules", schema: "employee");
            }
        }
    }
