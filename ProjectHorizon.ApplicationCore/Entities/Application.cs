namespace ProjectHorizon.ApplicationCore.Entities
{
    public abstract class Application : BaseEntity
    {
        public string? Architecture { get; set; }

        public string? Description { get; set; }

        public string? IconBase64 { get; set; }

        public int Id { get; set; }

        public string? InformationUrl { get; set; }

        public string? Language { get; set; }

        public string Name { get; set; }

        public string? Notes { get; set; }

        public string? Publisher { get; set; }

        public bool RunAs32Bit { get; set; }

        public string Version { get; set; }
    }
}