using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentSchedulePhaseDto
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int OffsetDays { get; set; }

        public long? AssignmentProfileId { get; set; }

        public string? AssignmentProfileName { get; set; }

        public long DeploymentScheduleId { get; set; }

        public bool UseRequirementScript { get; set; }
    }
}
