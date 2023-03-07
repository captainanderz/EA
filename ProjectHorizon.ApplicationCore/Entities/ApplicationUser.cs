using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsSuperAdmin { get; set; }

        public string NewUnconfirmedEmail { get; set; } = string.Empty;

        public string ProfilePictureSmall { get; set; } = string.Empty;

        public bool TwoFactorRequired { get; set; }

        public virtual List<SubscriptionUser> SubscriptionUsers { get; set; }

        public virtual List<Notification> Notifications { get; set; }

        public virtual ICollection<NotificationSetting> NotificationSettings { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public string LastAcceptedTermsVersion { get; set; } = "0.00.00";
    }
}