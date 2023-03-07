namespace ProjectHorizon.ApplicationCore.Enums
{
    public enum RequestState
    {
        NotSet = 0,

        /// <summary>
        /// The request has been accepted
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// The request has been rejected
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// The request is waiting for an answer
        /// </summary>
        Pending = 3
    }
}
