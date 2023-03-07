namespace ProjectHorizon.ApplicationCore.Enums
{
    public enum AssignmentType
    {
        /// <summary>
        /// Default value for the enum.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Makes the app required for the selected groups. 
        /// Required apps are installed automatically on enrolled devices. 
        /// Some platforms may have additional prompts for the end user to acknowledge before app installation begins.
        /// </summary>
        Required = 1,

        /// <summary>
        /// Makes the app available for the selected groups. 
        /// Available for enrolled devices apps are displayed in the Company Portal app and website for users to optionally install. 
        /// Available assignments are only valid for User Groups, not device groups.
        /// </summary>
        Available = 2,

        /// <summary>
        /// Makes the app uninstall for the selected groups. 
        /// Apps with this assignment are uninstalled from managed devices in the selected groups if Intune has previously installed the application onto the device via an "Available for enrolled devices" or "Required" assignment on the same deployment.
        /// </summary>
        Uninstall = 3,
    }
}