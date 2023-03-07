using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
    {
        public virtual void Configure(EntityTypeBuilder<UserInvitation> builder)
        {
            builder.Property(t => t.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.LastName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.Email)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.InvitationToken)
                .HasMaxLength(400)
                .IsRequired();

            builder.Property(t => t.UserRole)
                .HasMaxLength(20)
                .IsRequired();
        }
    }
}
