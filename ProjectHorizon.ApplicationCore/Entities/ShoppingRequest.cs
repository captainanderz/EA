using ProjectHorizon.ApplicationCore.Enums;
using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public abstract class ShoppingRequest : BaseEntity
    {
        public long Id { get; set; }

        public string RequesterId { get; set; }

        public string RequesterName { get; set; }

        public int ApplicationId { get; set; }

        public Guid SubscriptionId { get; set; }

        public RequestState StateId { get; set; } = RequestState.Pending;

        public DateTime ResolvedOn { get; set; }

        public string? AdminResolverId { get; set; }

        public virtual ApplicationUser? AdminResolver { get; set; }

        public string? ManagerResolverId { get; set; }

        public string? ManagerResolverName { get; set; }

        public bool IsValid { get; set; } = true;
    }
}
