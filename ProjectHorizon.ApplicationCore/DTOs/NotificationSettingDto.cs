using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class NotificationSettingDto
    {
        public Guid SubscriptionId { get; init; }

        public string ApplicationUserId { get; init; }

        public string NotificationType { get; init; }

        public bool IsEnabled { get; set; }
    }
}
