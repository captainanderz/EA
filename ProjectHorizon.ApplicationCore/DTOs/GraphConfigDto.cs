using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class GraphConfigDto
    {
        public int Id { get; set; }

        public Guid SubscriptionId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Tenant { get; set; }
    }
}
