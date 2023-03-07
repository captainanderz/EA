namespace ProjectHorizon.ApplicationCore.Enums
{
    /// <summary>
    /// Represents the different options available for "Delivery optimization priority", while
    /// * EndpointAdmin: adding a group to an assignment profile
    /// * Intune: Adding a group to an application
    /// </summary>
    public enum DeliveryOptimizationPriority
    {
        /// <summary>
        /// Default value for the enum.
        /// </summary>
        NotSet = 0,

        ContentDownloadInBackground = 1,

        ContentDownloadInForeground = 2,
    }
}