using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
            public void Configure(EntityTypeBuilder<User> builder)
            {
                builder.HasKey(u => u.Id);

                builder.ToTable("Users", schema: "identity")
                    .HasDiscriminator<string>("UserType")
                    .HasValue<User>("User")
                    .HasValue<Customer>("Customer")
                    .HasValue<Employee>("Employee");

                builder.Property("UserType")
                    .HasMaxLength(20)
                    .IsRequired();

                builder.Property(u => u.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(u => u.Email)
                    .HasMaxLength(100)
                    .IsRequired();

                builder.Property(u => u.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                builder.Property(u => u.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired(false);

                builder.Property(u => u.FirstName)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(u => u.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(u => u.IsActive)
                    .HasDefaultValue(true);

                builder.Property(u => u.RefreshToken)
                    .HasMaxLength(500)
                    .IsRequired(false);

                builder.Property(u => u.RefreshTokenExpiryTime)
                    .HasColumnType("datetime2");

                builder.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_User_Email");

                builder.HasIndex(u => u.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_User_Username");

                builder.HasIndex(u => u.PhoneNumber)
                    .HasDatabaseName("IX_User_Phone");

                builder.HasIndex(u => new { u.IsActive, u.RoleId })
                    .HasDatabaseName("IX_User_ActiveRole");

                builder.HasIndex("UserType")
                    .HasDatabaseName("IX_User_UserType");
            }
        }
}
