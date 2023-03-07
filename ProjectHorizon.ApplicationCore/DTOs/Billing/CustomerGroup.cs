using Newtonsoft.Json;

namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class CustomerGroup
    {
        [JsonProperty("customerGroupNumber")]
        public int CustomerGroupNumber { get; set; }
    }
}
