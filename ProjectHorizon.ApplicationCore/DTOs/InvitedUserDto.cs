using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class InvitedUserDto
    {
        public string Email { get; set; }

        public string SubscriptionName { get; set; }
    }
}