using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ShoppingApplicationDetailsDto : ShoppingApplicationDto
    {
        public string Developer { get; set; }

        public string InformationUrl { get; set; }

        public string Notes { get; set; }

        public string Architecture { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
