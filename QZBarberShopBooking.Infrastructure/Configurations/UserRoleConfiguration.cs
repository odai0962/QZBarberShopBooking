using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(ur => ur.Id);

            builder.HasIndex(ur => ur.Name)
                .IsUnique()
                .HasDatabaseName("IX_UserRole_Name");

            builder.HasMany(ur => ur.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ur => ur.RolePermissions)
                .WithOne(rp => rp.Role)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(ur => ur.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ur => ur.Description)
                .HasMaxLength(200);

            builder.ToTable("UserRoles", schema: "identity");
        }
    }
}
