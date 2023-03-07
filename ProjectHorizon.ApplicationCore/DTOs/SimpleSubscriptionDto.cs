using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class SimpleSubscriptionDto
    {
        public Guid Id { get; init; }

        public string LogoSmall { get; set; } = string.Empty;
    }
}
