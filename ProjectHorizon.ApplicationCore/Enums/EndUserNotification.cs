namespace ProjectHorizon.ApplicationCore.Enums
{
    /// <summary>
    /// Represents the different options available for "End user notifications", while 
    /// * EndpointAdmin: adding a group to an assignment profile
    /// * Intune: Adding a group to an application
    /// </summary>
    public enum EndUserNotification
    {
        /// <summary>
        /// Default value for the enum.
        /// </summary>
        NotSet = 0,

        ShowAllToastNotifications = 1,

        ShowToastNotificationsForComputerRestarts = 2,

        HideAllToastNotifications = 3,
    }
}