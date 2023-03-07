using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ApprovalDto : BaseEntity
    {
        public int Id { get; set; }

        public string DeployedVersion { get; set; }

        public string NewVersion { get; set; }

        public string Name { get; set; }

        public string IconBase64 { get; set; }

        public string IconFileName { get; set; }

        public string Architecture { get; set; }
    }
}
