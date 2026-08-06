using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasBaseType<User>();

            builder.Property(e => e.PhotoUrl)
                .HasMaxLength(500);
        }
    }
}
