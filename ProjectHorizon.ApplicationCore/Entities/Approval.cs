using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class Approval : BaseEntity
    {
        public int Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public virtual Subscription Subscription { get; set; }

        public int PublicApplicationId { get; set; }

        public virtual PublicApplication PublicApplication { get; set; }

        public bool IsActive { get; set; }

        public string? UserDecision { get; set; }

        public virtual SubscriptionPublicApplication SubscriptionPublicApplication { get; set; }
    }
}
