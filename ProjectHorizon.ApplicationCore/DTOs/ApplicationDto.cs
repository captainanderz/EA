using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ApplicationDto : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string Publisher { get; set; }

        public bool RunAs32Bit { get; set; }

        public string InformationUrl { get; set; }

        public string Notes { get; set; }

        public string IconBase64 { get; set; }

        public string IconFileName { get; set; }

        public string Language { get; set; }

        public string Architecture { get; set; }

        public string? AssignedProfileName { get; set; } = string.Empty;


        public bool IsInShop { get; set; }

        public string Description { get; set; }

        public string? ExistingVersion { get; set; }

        public DeploymentScheduleDto? AssignedDeploymentSchedule { get; set; }

        public bool AssignedDeploymentScheduleInProgress { get; set; }

        public string? AssignedDeploymentSchedulePhaseName { get; set; } = string.Empty;

        public PhaseState AssignedDeploymentSchedulePhaseState { get; set; }
    }
}
