using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class DeploymentSchedulePrivateApplicationConfiguration : IEntityTypeConfiguration<DeploymentSchedulePrivateApplication>
    {
        public void Configure(EntityTypeBuilder<DeploymentSchedulePrivateApplication> builder)
        {
            builder
                .HasOne(entity => entity.Application)
                .WithMany(entity => entity.DeploymentScheduleApplications)
                .HasForeignKey(entity => entity.ApplicationId)
                .OnDelete(DeleteBehavior.ClientCascade);

            builder
                .HasOne(entity => entity.DeploymentSchedule)
                .WithMany(entity => entity.DeploymentSchedulePrivateApplications)
                .HasForeignKey(entity => entity.DeploymentScheduleId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
