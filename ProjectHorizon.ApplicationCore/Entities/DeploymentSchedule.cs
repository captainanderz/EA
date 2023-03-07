using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class DeploymentSchedule : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid SubscriptionId { get; set; }

        public virtual Subscription Subscription { get; set; } = null!;

        public string? CronTrigger { get; set; }

        public int? CurrentPhaseIndex { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<DeploymentSchedulePhase> DeploymentSchedulePhases { get; set; } = null!;

        public virtual ICollection<PrivateApplication> PrivateApplications { get; set; } = null!;

        public virtual ICollection<SubscriptionPublicApplication> SubscriptionPublicApplications { get; set; } = null!;

        public virtual ICollection<DeploymentSchedulePrivateApplication> DeploymentSchedulePrivateApplications { get; set; } = null!;

        public virtual ICollection<DeploymentScheduleSubscriptionPublicApplication> DeploymentScheduleSubscriptionPublicApplications { get; set; } = null!;

        public virtual ICollection<AssignmentProfile> AssignmentProfiles { get; set; } = null!;
    }
}
