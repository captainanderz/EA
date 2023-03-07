using Newtonsoft.Json;

namespace ProjectHorizon.ApplicationCore.DTOs.Billing
{
    public class VatZone
    {
        [JsonProperty("vatZoneNumber")]
        public int VatZoneNumber { get; set; }
    }
}
