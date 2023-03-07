using ProjectHorizon.ApplicationCore.Constants;
using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class RegistrationDto : BillingInfoDto
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Password { get; set; }

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "The terms must be accepted.")]
        public bool AcceptedTerms { get; set; }
    }
}