using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class AssignmentProfileDetailsDto : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public int NumberOfApplicationsAssigned { get; set; }

        public AssignmentProfileGroupDetailsDto[] Groups { get; set; }
    }
}
