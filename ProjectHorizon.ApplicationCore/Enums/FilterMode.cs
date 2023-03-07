namespace ProjectHorizon.ApplicationCore.Enums
{
    public enum FilterMode
    {
        /// <summary>
        /// Default value for the enum, also used for "Do not apply filter".
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Include filtered devices in assignment.
        /// </summary>
        Include = 1,

        /// <summary>
        /// Exclude filtered devices in assignment.
        /// </summary>
        Exclude = 2,
    }
}