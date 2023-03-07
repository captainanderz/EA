using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class AuditLog : BaseEntity
    {
        public string ActionText { get; set; }

        public virtual ApplicationUser? ApplicationUser { get; set; }

        public string? ApplicationUserId { get; set; }

        public string Category { get; set; }

        public int Id { get; set; }

        public string SourceIP { get; set; }

        public virtual Subscription Subscription { get; set; }

        public Guid? SubscriptionId { get; set; }
    }
}