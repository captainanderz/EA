namespace ProjectHorizon.ApplicationCore.Constants
{
    public static class AuditLogActions
    {
        public const string AssignmentProfilesAddProfile = "The user added the profile {0} to the assignment profiles.";
        public const string AssignmentProfilesRemoveProfile = "The user removed the profile {0} from the assignment profiles.";
        public const string AssignmentProfilesAssignProfile = "The user assigned the profile {0} to the application {1}.";
        public const string AssignmentProfilesClearProfile = "The user cleared the assigned profile of the application {0}.";
        public const string AssignmentProfilesCopyProfile = "The user copied the assignment profile {0}.";
        public const string AssignmentProfilesEditProfile = "The user edited the assignment profile {0}.";

        public const string DeploymentSchedulesAddSchedule = "The user added the schedule {0} to the deployment schedules.";
        public const string DeploymentSchedulesRemoveSchedule = "The user removed the schedule {0} from the deployment schedules.";
        public const string DeploymentSchedulesAssignSchedule = "The user assigned the schedule {0} to the application {1}.";
        public const string DeploymentSchedulesClearSchedule = "The user cleared the assigned schedule of the application {0}.";
        public const string DeploymentSchedulesCopySchedule = "The user copied the deployment schedule {0}.";
        public const string DeploymentSchedulesEditSchedule = "The user edited the deployment schedule {0}.";
        public const string DeploymentSchedulesStartSchedule = "The deployment schedule {0} has started.";
        public const string DeploymentSchedulesFailedStartSchedule = "The deployment schedule {0} failed to start.";

        public const string RepositoryAddApp = "The user added the application {0} to the {1} repository.";
        public const string RepositoryNewVersionApp = "The user added a new version of the application {0} to the {1} repository.";
        public const string RepositoryRemoveApp = "The user removed application {0} from the {1} repository.";
        public const string RepositoryDeployApp = "The user started the deploy of application {0} from the {1} repository.";
        public const string RepositoryDownloadApp = "The user downloaded application {0} from the {1} repository.";

        public const string NotificationSetting = "The user has {0} the {1} notification for user {2}.";
        public const string NotificationSettingsBulk = "The user {0} all notifications for users: {1}.";

        public const string GraphConfigCreated = "Integration with Microsoft Endpoint Manager has been created.";
        public const string GraphConfigRemoved = "Integration with Microsoft Endpoint Manager has been removed.";

        public const string Approvals = "The user has {0} the deployment of the {1} application.";

        public const string UserInvitation = "The user has invited {0} on the {1} subscription.";
        public const string UserChangeRole = "The role of the {0} user has been updated to {1}.";
        public const string UserRemoved = "The user removed {0} from this subscription.";

        public const string SubscriptionCancelled = "The user cancelled the subscription.";
        public const string SubscriptionReactivated = "The user reactivated the subscription.";

        public const string ShopRequestAdminApproved = "An admin has accepted {0}'s request for {1}.";
        public const string ShopRequestAdminRejected = "An admin has rejected {0}'s request for {1}.";
        public const string ShopRequestManagerApproved = "{0} has accepted {1}'s request for {2}.";
        public const string ShopRequestManagerRejected = "{0} has rejected {1}'s request for {2}.";
    }
}
