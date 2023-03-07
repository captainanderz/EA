using System;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class SubscriptionPublicApplication : BaseEntity
    {
        public virtual AssignmentProfile? AssignmentProfile { get; set; }

        /// <summary>
        /// Reference to the assignment profile. Nullable to handle the case, when there is no linked profile.
        /// </summary>
        public long? AssignmentProfileId { get; set; }

        public virtual DeploymentSchedule? DeploymentSchedule { get; set; }

        public long? DeploymentScheduleId { get; set; }

        public virtual ICollection<DeploymentScheduleSubscriptionPublicApplication> DeploymentScheduleApplications { get; set; }

        public bool AutoUpdate { get; set; }

        public string? DeployedVersion { get; set; }

        public string? DeploymentStatus { get; set; }

        public string? IntuneId { get; set; }

        public bool ManualApprove { get; set; }

        public virtual PublicApplication PublicApplication { get; set; }

        public int PublicApplicationId { get; set; }

        public virtual Subscription Subscription { get; set; }

        public Guid SubscriptionId { get; set; }

        public bool IsInShop { get; set; }

        public virtual AzureGroup? ShopGroup { get; set; }

        public string? ShopGroupId { get; set; }
    }
}