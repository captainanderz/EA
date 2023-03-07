namespace ProjectHorizon.ApplicationCore.Constants
{
    public class AuditLogCategory : BaseEnum<AuditLogCategory>
    {
        public const string AllCategories = "All Categories";
        public const string PublicRepository = "Public Repository";
        public const string PrivateRepository = "Private Repository";
        public const string Approvals = "Approvals";
        public const string UserManagement = "User Management";
        public const string NotificationSettings = "Notification Settings";
        public const string Integrations = "Integrations";
        public const string SingleSignOn = "Single Sign-On";
        public const string Subscription = "Subscription";
        public const string AssignmentProfiles = "Assignment Profiles";
        public const string ShopRequests = "Shop Requests";
        public const string DeploymentSchedules = "Deployment Schedules";
    }
}   