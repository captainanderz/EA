using System;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public string ProfilePictureSmall { get; set; }

        public Guid SubscriptionId { get; set; }

        public string UserRole { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string SourceIP { get; set; }

        public bool TwoFactorRequired { get; set; }

        public IEnumerable<UserSubscriptionDto> Subscriptions { get; set; } = new List<UserSubscriptionDto>(0);
    }
}