using ProjectHorizon.ApplicationCore.Enums;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ShoppingApplicationDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string Publisher { get; set; }

        public string? IconBase64 { get; set; }

        public string? Description { get; set; }

        public bool IsPrivate { get; set; }
        public RequestState RequestState { get; set; }
    }
}
