using ProjectHorizon.ApplicationCore.Entities;
using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class SubscriptionDto : BaseEntity
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public string State { get; init; }

        public int NumberOfUsers { get; set; }

        public int? DeviceCount { get; set; }

        public string FarPayToken { get; set; }

        public bool GlobalAutoUpdate { get; init; }

        public bool GlobalManualApprove { get; init; }

        public string LogoSmall { get; set; } = string.Empty;

        public bool AzureIntegrationDone { get; set; }

        public string ShopGroupPrefix { get; set; } = string.Empty;
    }
}
