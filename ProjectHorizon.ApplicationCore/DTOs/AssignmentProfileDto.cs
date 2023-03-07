using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class AssignmentProfileDto : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public int NumberOfApplicationsAssigned { get; set; }

        public int NumberOfDeploymentSchedules { get; set; }
    }
}
