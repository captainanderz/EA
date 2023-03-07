using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentScheduleDto : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public int NumberOfApplicationsAssigned { get; set; }

        public string? CurrentPhaseName { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsInProgress { get; set; }

        public string? CronTrigger { get; set; }
    }
}
