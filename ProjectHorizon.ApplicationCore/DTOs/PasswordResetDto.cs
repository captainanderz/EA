using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class PasswordResetDto
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
