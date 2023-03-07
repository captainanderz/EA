using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class Notification : BaseEntity
    {
        public virtual ApplicationUser ApplicationUser { get; set; }

        public string? ApplicationUserId { get; set; }

        public bool ForPrivateRepository { get; set; }

        public long Id { get; set; }

        public bool IsRead { get; set; }

        public string Message { get; set; }

        public virtual Subscription Subscription { get; set; }

        public Guid SubscriptionId { get; set; }

        public string Type { get; set; }
    }
}