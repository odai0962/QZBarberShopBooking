using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class PagePlatformConfiguration : IEntityTypeConfiguration<PagePlatform>
    {
        public void Configure(EntityTypeBuilder<PagePlatform> builder)
        {
            builder.HasKey(pp => pp.Id);

            builder.HasAlternateKey(pp => new { pp.PageId, pp.Platform });

            builder.HasIndex(pp => pp.PageId)
                .HasDatabaseName("IX_PagePlatform_Page");

            builder.HasOne(pp => pp.Page)
                .WithMany(p => p.PagePlatforms)
                .HasForeignKey(pp => pp.PageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(pp => pp.Platform)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.ToTable("PagePlatforms", schema: "system");
        }
    }
}
