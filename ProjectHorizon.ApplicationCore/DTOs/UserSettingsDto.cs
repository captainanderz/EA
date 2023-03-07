using ProjectHorizon.ApplicationCore.Constants;
using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class UserSettingsDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string LastName { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}