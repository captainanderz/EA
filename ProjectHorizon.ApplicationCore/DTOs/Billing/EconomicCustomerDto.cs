using Newtonsoft.Json;

namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class EconomicCustomerDto
    {
        [JsonIgnore]
        public string CustomerNumber { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("paymentTerms")]
        public PaymentTerms PaymentTerms { get; set; }

        [JsonProperty("customerGroup")]
        public CustomerGroup CustomerGroup { get; set; }

        [JsonProperty("vatZone")]
        public VatZone VatZone { get; set; }

        [JsonProperty("vatNumber")]
        public string VatNumber { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("zip")]
        public string Zip { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
