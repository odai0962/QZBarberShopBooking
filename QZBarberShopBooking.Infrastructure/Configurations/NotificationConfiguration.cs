using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
                .HasDatabaseName("IX_Notification_RecipientRead");

            builder.HasIndex(n => new { n.RecipientUserId, n.CreationDate })
                .HasDatabaseName("IX_Notification_RecipientCreated");

            builder.HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.RelatedBooking)
                .WithMany()
                .HasForeignKey(n => n.RelatedBookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(40);

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(n => n.Body)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false);

            builder.ToTable("Notifications", schema: "notification");
        }
    }
}
