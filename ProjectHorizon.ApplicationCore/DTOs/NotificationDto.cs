using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class NotificationDto : BaseEntity
    {
        public long Id { get; set; }

        public string Type { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; }

        public bool ForPrivateRepository { get; set; }
    }
}
