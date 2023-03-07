using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class RegisterInvitationDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string EmailToken { get; set; }

        public string SubscriptionName { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "The terms must be accepted.")]
        public bool AcceptedTerms { get; set; }
    }
}