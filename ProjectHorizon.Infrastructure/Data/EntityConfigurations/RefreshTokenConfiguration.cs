using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public virtual void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.Property(t => t.ApplicationUserId)
                .IsRequired();

            builder.Property(t => t.Token)
                .IsRequired();
        }
    }
}
