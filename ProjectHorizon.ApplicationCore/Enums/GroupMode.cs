namespace ProjectHorizon.ApplicationCore.Enums
{
    public enum GroupMode
    {
        /// <summary>
        /// Default value for the enum.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Specifies that the azure group is included on the assignment type.
        /// </summary>
        Included = 1,

        /// <summary>
        /// Specifies that the azure group is excluded from the assignment type.
        /// </summary>
        Excluded = 2,

        /// <summary>
        /// Specifies that all users should be included in the assignment type.
        /// </summary>
        AllUsers = 3,

        /// <summary>
        /// Specifies that all devices should be included in the assignment type.
        /// </summary>
        AllDevices = 4,
    }
}