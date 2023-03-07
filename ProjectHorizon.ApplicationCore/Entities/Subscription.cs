using System;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class Subscription : BaseEntity
    {
        public string City { get; set; }

        public string CompanyName { get; set; }

        public string Country { get; set; }

        public string? CreditCardDigits { get; set; }

        public string? CustomerNumber { get; set; }

        public int? DeviceCount { get; set; }

        public string Email { get; set; }

        public string? FarPayToken { get; set; }

        public bool GlobalAutoUpdate { get; set; }

        public bool GlobalManualApprove { get; set; }

        public virtual GraphConfig GraphConfig { get; set; }

        public int? GraphConfigId { get; set; }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public virtual List<PrivateApplication> PrivateApplications { get; set; }

        public string State { get; set; }

        public virtual List<SubscriptionUser> SubscriptionUsers { get; set; }

        public string VatNumber { get; set; }

        public string ZipCode { get; set; }

        public string LogoSmall { get; set; } = string.Empty;

        public virtual ICollection<SubscriptionPublicApplication> SubscriptionPublicApplications { get; set; }

        public virtual ICollection<SubscriptionConsent> Consents { get; set; } = null!;

        public string ShopGroupPrefix { get; set; } = string.Empty;
    }
}