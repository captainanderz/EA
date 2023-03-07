using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class DeploymentSchedulePhase : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int OffsetDays { get; set; }

        public int Index { get; set; }

        public long DeploymentScheduleId { get; set; }

        public virtual DeploymentSchedule DeploymentSchedule { get; set; } = null!;

        public long? AssignmentProfileId { get; set; }

        public virtual AssignmentProfile? AssignmentProfile { get; set; }

        public bool UseRequirementScript { get; set; }

        public virtual ICollection<DeploymentSchedulePrivateApplication> DeploymentSchedulePrivateApplications { get; set; } = null!;

        public virtual ICollection<DeploymentScheduleSubscriptionPublicApplication> DeploymentScheduleSubscriptionPublicApplications { get; set; } = null!;
    }
}
