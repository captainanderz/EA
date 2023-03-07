using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentScheduleDetailsDto : BaseEntity
    {
        public long? Id { get; set; }

        public string Name { get; set; }

        public DeploymentSchedulePhaseDto[] Phases { get; set; }

        public string? CronTrigger { get; set; }
    }
}
