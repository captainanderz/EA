using SendGrid.Helpers.Mail;
using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class EmailDetailsDto
    {
        public string FromEmail { get; set; }

        public string FromName { get; set; }

        public string ToEmail { get; set; }

        public string ToName { get; set; }

        public string Subject { get; set; }

        public string HTMLContent { get; set; }

        public List<Attachment> Attachments { get; set; }
    }
}
