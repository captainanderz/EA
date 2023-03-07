using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class DeploymentScheduleSubscriptionPublicApplicationConfiguration : IEntityTypeConfiguration<DeploymentScheduleSubscriptionPublicApplication>
    {
        public void Configure(EntityTypeBuilder<DeploymentScheduleSubscriptionPublicApplication> builder)
        {
            builder
                .HasOne(entity => entity.Application)
                .WithMany(entity => entity.DeploymentScheduleApplications)
                .HasForeignKey(entity => new { entity.SubscriptionId, entity.ApplicationId })
                .OnDelete(DeleteBehavior.ClientCascade);

            builder
                .HasOne(entity => entity.Subscription)
                .WithMany()
                .HasForeignKey(entity => entity.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientCascade);

            builder
                .HasOne(entity => entity.DeploymentSchedule)
                .WithMany(entity => entity.DeploymentScheduleSubscriptionPublicApplications)
                .HasForeignKey(entity => entity.DeploymentScheduleId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
