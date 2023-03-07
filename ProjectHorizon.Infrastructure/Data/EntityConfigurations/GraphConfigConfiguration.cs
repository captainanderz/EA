using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class GraphConfigConfiguration : IEntityTypeConfiguration<GraphConfig>
    {
        public virtual void Configure(EntityTypeBuilder<GraphConfig> builder)
        {
            builder.Property(t => t.ClientId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.ClientSecret)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.Tenant)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
