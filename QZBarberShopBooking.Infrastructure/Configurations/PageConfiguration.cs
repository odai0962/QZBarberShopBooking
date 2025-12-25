using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Page_Code");

            builder.HasIndex(p => p.Url)
                .HasDatabaseName("IX_Page_Url");

            builder.Property(p => p.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.PageName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.Icon)
                .HasMaxLength(50);

            builder.Property(p => p.Url)
                .HasMaxLength(200)
                .IsRequired();

            builder.ToTable("Pages", schema: "system");
        }
    }
}
