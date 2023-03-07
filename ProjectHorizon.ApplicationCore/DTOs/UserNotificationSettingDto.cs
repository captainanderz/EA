namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class UserNotificationSettingDto
    {
        public string UserId { get; init; }

        public string FullName { get; init; }

        public string ProfilePictureSmall { get; set; }

        public bool IsNewApplication { get; set; } = true;

        public bool IsNewVersion { get; set; } = true;

        public bool IsSuccessfulDeployment { get; set; } = true;

        public bool IsFailedDeployment { get; set; } = true;

        public bool IsManualApproval { get; set; } = true;

        public bool IsDeletedApplication { get; set; } = true;

        public bool IsShop { get; set; } = true;

        public bool IsAssignmentProfiles { get; set; } = true;

        public bool IsDeploymentSchedules { get; set; } = true;
    }
}
