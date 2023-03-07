using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class AuditLogDto : BaseEntity
    {
        public int Id { get; set; }

        public string Category { get; set; }

        public string ActionText { get; set; }

        public string SourceIP { get; set; }

        public string User { get; set; }
    }
}