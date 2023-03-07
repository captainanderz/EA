using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class SubscriptionPublicApplicationConfiguration : IEntityTypeConfiguration<SubscriptionPublicApplication>
    {
        public virtual void Configure(EntityTypeBuilder<SubscriptionPublicApplication> builder)
        {
            builder.HasKey(t => new { t.SubscriptionId, t.PublicApplicationId });

            builder.Property(t => t.DeployedVersion)
                .HasMaxLength(100);

            builder.Property(t => t.IntuneId)
                .HasMaxLength(50);

            builder.Property(t => t.DeploymentStatus)
                .HasMaxLength(50);

            builder.HasOne(entity => entity.AssignmentProfile);

            builder.HasOne(entity => entity.ShopGroup);
        }
    }
}
