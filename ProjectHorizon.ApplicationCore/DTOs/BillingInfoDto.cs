using ProjectHorizon.ApplicationCore.Constants;
using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class BillingInfoDto
    {
        [Required]
        [RegularExpression(Patterns.CompanyName)]
        public string CompanyName { get; set; }

        [Required]
        public string SubscriptionEmail { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string ZipCode { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string VatNumber { get; set; }
    }
}