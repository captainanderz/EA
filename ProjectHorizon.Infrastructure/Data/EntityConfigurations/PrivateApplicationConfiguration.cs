using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class PrivateApplicationConfiguration : ApplicationConfiguration<PrivateApplication>
    {
        public override void Configure(EntityTypeBuilder<PrivateApplication> builder)
        {
            builder.Property(t => t.PrivateRepositoryArchiveFileName)
                .HasMaxLength(1000);

            builder.Property(t => t.DeployedVersion)
                .HasMaxLength(100);

            builder.Property(t => t.IntuneId)
                .HasMaxLength(50);

            builder.Property(t => t.DeploymentStatus)
                .HasMaxLength(50);

            builder.HasOne(entity => entity.AssignmentProfile);

            builder.HasOne(entity => entity.ShopGroup);

            base.Configure(builder);
        }
    }
}