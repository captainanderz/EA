using ProjectHorizon.ApplicationCore.Enums;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class DeploymentScheduleApplication : BaseEntity
    {
        public long Id { get; set; }

        public int ApplicationId { get; set; }

        public long DeploymentScheduleId { get; set; }

        public virtual DeploymentSchedule DeploymentSchedule { get; set; } = null!;

        public DeploymentScheduleApplicationType Type { get; set; }

        public string ArchiveFileName { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string IntuneId { get; set; } = string.Empty;

        public long? CurrentPhaseId { get; set; }

        public virtual DeploymentSchedulePhase? CurrentPhase { get; set; } = null!;

        public PhaseState PhaseState { get; set; }
    }
}
