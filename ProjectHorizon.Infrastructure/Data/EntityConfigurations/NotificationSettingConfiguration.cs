using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
    {
        public virtual void Configure(EntityTypeBuilder<NotificationSetting> builder)
        {
            builder.HasKey(t => new { t.SubscriptionId, t.ApplicationUserId, t.NotificationType });

            builder.Property(t => t.NotificationType)
                .HasMaxLength(40)
                .IsRequired();
        }
    }
}
