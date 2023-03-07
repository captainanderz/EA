using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public virtual void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(t => t.Type)
                .HasMaxLength(40)
                .IsRequired();

            builder.Property(t => t.Message)
                .HasMaxLength(500)
                .IsRequired();
        }
    }
}
