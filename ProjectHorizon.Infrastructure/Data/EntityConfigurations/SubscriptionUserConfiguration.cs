using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class SubscriptionUserConfiguration : IEntityTypeConfiguration<SubscriptionUser>
    {
        public virtual void Configure(EntityTypeBuilder<SubscriptionUser> builder)
        {
            builder.HasKey(t => new { t.SubscriptionId, t.ApplicationUserId });

            builder.Property(t => t.UserRole)
                .HasMaxLength(20)
                .IsRequired();
        }
    }
}
