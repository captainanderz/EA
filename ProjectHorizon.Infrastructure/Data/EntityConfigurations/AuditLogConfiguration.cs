using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public virtual void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(t => t.Category)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.ActionText)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(t => t.SourceIP)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
