using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface ITermsService
    {
        /// <summary>
        /// Checks if the last terms version was accepted for the current user
        /// </summary>
        /// <returns>A bool determining if the new terms version was accepted or not</returns>
        Task<bool> CheckAcceptedTermsLastVersionAsync();

        /// <summary>
        /// Accepts the newest terms version
        /// </summary>
        /// <returns>A status code representing the status of the action</returns>
        Task<int> AcceptTermsAsync();
    }
}
