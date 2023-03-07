using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class AssignmentProfileApplicationDto
    {
        public Guid SubscriptionId { get; set; }

        public string IntuneId { get; set; }

        public string Name { get; set; }

        public bool IsPrivate { get; set; }
    }
}
