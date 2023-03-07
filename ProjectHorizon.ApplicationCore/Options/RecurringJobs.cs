using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.Options
{
    public class RecurringJobs
    {
        public bool SendUnreadNotificationEmails { get; init; }

        [Range(0, 23, ErrorMessage = "Value range is 0-23")]
        public int SendUnreadNotificationEmailsHour { get; init; }

        [Range(0, 23, ErrorMessage = "Value range is 0-23")]
        public int UpdateDeviceCountHour { get; init; }
    }
}
