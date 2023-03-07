using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class AssignmentProfileGroupConfiguration : IEntityTypeConfiguration<AssignmentProfileGroup>
    {
        public virtual void Configure(EntityTypeBuilder<AssignmentProfileGroup> builder)
        {
            builder.HasKey(entity => entity.Id);
            builder.HasComment("A collection of groups defined by IT-administrators to deploy one or more applications to");

            builder.HasIndex(entity => new { entity.AssignmentProfileId, entity.AzureGroupId, entity.AssignmentTypeId, entity.GroupModeId })
                .IsUnique()
                .HasFilter(null);

            builder.Property(entity => entity.AssignmentProfileId)
                .IsRequired();
            builder.Property(entity => entity.AzureGroupId)
                .HasMaxLength(68);
            builder.Property(entity => entity.AssignmentTypeId)
                .IsRequired();

            builder.Property(entity => entity.FilterId)
                .HasMaxLength(68);
        }
    }
}