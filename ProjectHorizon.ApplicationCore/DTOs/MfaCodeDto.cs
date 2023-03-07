using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class MfaCodeDto
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string Code { get; set; }
    }
}