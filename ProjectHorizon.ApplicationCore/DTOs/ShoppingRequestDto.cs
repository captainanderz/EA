using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ShoppingRequestDto : BaseEntity
    {
        public long Id { get; set; }

        public string ApplicationName { get; set; }

        public string RequesterId { get; set; }

        public string RequesterName { get; set; }

        public string ResolverName { get; set; }

        public RequestState StateId { get; set; }

        public int ApplicationId { get; set; }

        public Guid SubscriptionId { get; set; }

        public bool IsPrivate { get; set; }
    }
}
