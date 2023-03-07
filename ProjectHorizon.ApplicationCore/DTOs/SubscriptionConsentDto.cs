using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class SubscriptionConsentDto : BaseEntity
    {
        public string TenantId { get; set; } = null!;
    }
}