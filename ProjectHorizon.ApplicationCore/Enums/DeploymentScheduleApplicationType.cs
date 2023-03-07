namespace ProjectHorizon.ApplicationCore.Enums
{
    public enum DeploymentScheduleApplicationType
    {
        /// <summary>
        /// Default value for the enum.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// The patch app from the current deployment schedule run
        /// </summary>
        Current = 1,

        /// <summary>
        /// The patch app from the previous deployment schedule run
        /// </summary>
        Previous = 2,
    }
}
