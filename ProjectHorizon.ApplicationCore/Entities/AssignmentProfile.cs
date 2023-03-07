using System;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    /// <summary>
    /// Endpoint Admin Assignment Profiles 
    /// A collection of groups defined by IT-administrators to deploy one or more applications to.
    /// Endpoint Admin will make use of the assignment functionality in Intune as well as reading Azure AD groups from the Microsoft Graph API 
    /// allowing users to configure assignments for an individual group.Documentation on how to archive this is found in the references section.
    /// The assignment functionality can be found on a given Win32 app on a given tenant on the following url: https://endpoint.microsoft.com 
    /// and comes in 3 different intentions and with multiple options:
    /// </summary>
    public class AssignmentProfile : BaseEntity
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public virtual Subscription? Subscription { get; set; }

        public Guid SubscriptionId { get; set; }

        public virtual ICollection<DeploymentSchedulePhase> DeploymentSchedulePhases { get; set; }
        public virtual ICollection<DeploymentSchedule> DeploymentSchedules { get; set; }
    }
}