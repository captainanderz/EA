using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class DeploymentScheduleConfiguration : IEntityTypeConfiguration<DeploymentSchedule>
    {
        public virtual void Configure(EntityTypeBuilder<DeploymentSchedule> builder)
        {
            builder
                .Property(entity => entity.Name)
                .HasMaxLength(Validation.NameMaxLength)
                .IsRequired();

            builder
                .HasMany(entity => entity.DeploymentSchedulePhases)
                .WithOne(entity => entity.DeploymentSchedule)
                .HasForeignKey(entity => entity.DeploymentScheduleId);

            builder
            .HasMany(entity => entity.AssignmentProfiles)
            .WithMany(entity => entity.DeploymentSchedules)
            .UsingEntity<DeploymentSchedulePhase>(
                j => j
                    .HasOne(phase => phase.AssignmentProfile)
                    .WithMany(assignmentProfile => assignmentProfile.DeploymentSchedulePhases)
                    .HasForeignKey(phase => phase.AssignmentProfileId)
                    .OnDelete(DeleteBehavior.SetNull),
                j => j
                    .HasOne(phase => phase.DeploymentSchedule)
                    .WithMany(deploymentSchedule => deploymentSchedule.DeploymentSchedulePhases)
                    .HasForeignKey(phase => phase.DeploymentScheduleId)
                    .OnDelete(DeleteBehavior.ClientCascade)
            );
        }
    }
}
