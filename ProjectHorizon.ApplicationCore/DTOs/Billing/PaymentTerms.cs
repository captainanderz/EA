using Newtonsoft.Json;

namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class PaymentTerms
    {
        [JsonProperty("paymentTermsNumber")]
        public int PaymentTermsNumber { get; set; }
    }
}
