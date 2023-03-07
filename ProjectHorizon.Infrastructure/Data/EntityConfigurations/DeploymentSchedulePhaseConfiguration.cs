using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.Infrastructure.Data.EntityConfigurations
{
    public class DeploymentSchedulePhaseConfiguration : IEntityTypeConfiguration<DeploymentSchedulePhase>
    {
        public virtual void Configure(EntityTypeBuilder<DeploymentSchedulePhase> builder)
        {

        }
    }
}