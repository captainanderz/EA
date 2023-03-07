using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class NotificationSetting : BaseEntity
    {
        public Guid SubscriptionId { get; set; }
        public virtual Subscription Subscription { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public string NotificationType { get; set; }

        public bool IsEnabled { get; set; }
    }
}
