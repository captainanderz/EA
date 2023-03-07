using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class BulkNotificationSettingsDto
    {
        public IEnumerable<string> UserIds { get; init; }

        public bool IsEnabled { get; init; }
    }
}
