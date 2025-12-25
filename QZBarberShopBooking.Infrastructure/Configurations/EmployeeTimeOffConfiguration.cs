using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class EmployeeTimeOffConfiguration : IEntityTypeConfiguration<EmployeeTimeOff>
    {
        public void Configure(EntityTypeBuilder<EmployeeTimeOff> builder)
        {
            builder.HasKey(et => et.Id);

            builder.HasIndex(et => new { et.EmployeeId, et.StartDate, et.EndDate })
                .HasDatabaseName("IX_EmployeeTimeOff_EmployeeDateRange");

            builder.HasIndex(et => et.StartDate)
                .HasDatabaseName("IX_EmployeeTimeOff_StartDate");

            builder.HasOne(et => et.Employee)
                .WithMany(e => e.TimeOffs)
                .HasForeignKey(et => et.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(et => et.Reason)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(et => et.StartDate)
                .HasColumnType("datetime2");

            builder.Property(et => et.EndDate)
                .HasColumnType("datetime2");

            builder.Property(et => et.IsApproved)
                .HasDefaultValue(false);

            builder.ToTable(tb => tb.HasCheckConstraint(
                "CK_EmployeeTimeOff_DateRange",
                "[EndDate] >= [StartDate]"
            ));

            builder.ToTable("EmployeeTimeOffs", schema: "employee");
        }
    }
}
