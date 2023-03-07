using System;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class PrivateApplication : Application
    {
        public virtual AssignmentProfile? AssignmentProfile { get; set; }

        /// <summary>
        /// Reference to the assignment profile. Nullable to handle the case, when there is no linked profile.
        /// </summary>
        public long? AssignmentProfileId { get; set; }

        public virtual DeploymentSchedule? DeploymentSchedule { get; set; }

        public long? DeploymentScheduleId { get; set; }

        public virtual ICollection<DeploymentSchedulePrivateApplication> DeploymentScheduleApplications { get; set; }

        public string? DeployedVersion { get; set; }

        public string? DeploymentStatus { get; set; }

        public string? IntuneId { get; set; }

        public string? PrivateRepositoryArchiveFileName { get; set; }

        public virtual Subscription Subscription { get; set; }

        public Guid SubscriptionId { get; set; }

        public bool IsInShop { get; set; }

        public virtual AzureGroup? ShopGroup { get; set; }

        public string? ShopGroupId { get; set; }
    }
}