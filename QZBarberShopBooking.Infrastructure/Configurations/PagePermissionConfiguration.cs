using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class PagePermissionConfiguration : IEntityTypeConfiguration<PagePermission>
    {
        public void Configure(EntityTypeBuilder<PagePermission> builder)
        {
            builder.HasKey(pp => pp.Id);

            builder.HasAlternateKey(pp => new { pp.PageId, pp.PermissionId });

            builder.HasIndex(pp => pp.PageId)
                .HasDatabaseName("IX_PagePermission_Page");

            builder.HasIndex(pp => pp.PermissionId)
                .HasDatabaseName("IX_PagePermission_Permission");

            builder.HasOne(pp => pp.Page)
                .WithMany(p => p.PagePermissions)
                .HasForeignKey(pp => pp.PageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pp => pp.Permission)
                .WithMany(perm => perm.PagePermissions)
                .HasForeignKey(pp => pp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("PagePermissions", schema: "system");
        }
    }
}
