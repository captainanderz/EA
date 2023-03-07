namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class FarPayCustomerDto
    {
        public string CustomerNumber { get; set; }

        public string CompanyNo { get; set; }

        public string Gln { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PoBox { get; set; }

        public string Street { get; set; }

        public string AdditionalStreet { get; set; }

        public string HouseNumber { get; set; }

        public string PostCode { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public bool AttachPdfInvoice { get; set; }

        public string Language { get; set; }
    }
}
