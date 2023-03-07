using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(t => t.FirstName)
                .HasMaxLength(50);

            builder.Property(t => t.LastName)
                .HasMaxLength(50);

            builder.Property(t => t.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(t => t.NewUnconfirmedEmail)
                .HasMaxLength(256);
        }
    }
}