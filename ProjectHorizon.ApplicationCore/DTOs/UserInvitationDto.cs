using ProjectHorizon.ApplicationCore.Constants;
using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class UserInvitationDto
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        [RegularExpression(Patterns.PersonName)]
        public string LastName { get; set; }

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string UserRole { get; set; }
    }
}