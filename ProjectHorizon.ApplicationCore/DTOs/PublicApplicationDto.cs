namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class PublicApplicationDto : ApplicationDto
    {
        public bool AutoUpdate { get; set; }

        public bool ManualApprove { get; set; }

        public string? DeploymentStatus { get; set; }
    }
}
