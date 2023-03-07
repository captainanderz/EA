using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class SimpleUserDto
    {
        public string Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; } = string.Empty;
    }
}
