using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
    {
        public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.HasIndex(t => t.DeviceToken)
                .IsUnique()
                .HasDatabaseName("IX_UserDeviceToken_Token");

            builder.HasIndex(t => new { t.UserId, t.IsActive })
                .HasDatabaseName("IX_UserDeviceToken_UserActive");

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(t => t.DeviceToken)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(t => t.DeviceType)
                .HasMaxLength(20);

            builder.Property(t => t.IsActive)
                .HasDefaultValue(true);

            builder.ToTable("UserDeviceTokens", schema: "notification");
        }
    }
}
