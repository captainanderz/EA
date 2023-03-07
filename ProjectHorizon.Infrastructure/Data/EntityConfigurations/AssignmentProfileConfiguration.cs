using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class AssignmentProfileConfiguration : IEntityTypeConfiguration<AssignmentProfile>
    {
        public virtual void Configure(EntityTypeBuilder<AssignmentProfile> builder)
        {
            builder.HasKey(entity => entity.Id);

            builder.Property(entity => entity.Name)
                .HasMaxLength(Validation.NameMaxLength)
                .IsRequired();
        }
    }
}
