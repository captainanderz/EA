using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class TermsService : ITermsService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ApplicationInformation _applicationInformation;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        public TermsService(
            IOptions<ApplicationInformation> applicationInformation,
            IApplicationDbContext applicationDbContext,
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationInformation = applicationInformation.Value;
            _applicationDbContext = applicationDbContext;
            _loggedInUserProvider = loggedInUserProvider;
        }

        /// <summary>
        /// Checks if the last terms version was accepted for the current user
        /// </summary>
        /// <returns>A bool determining if the new terms version was accepted or not</returns>
        public async Task<bool> CheckAcceptedTermsLastVersionAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Entities.ApplicationUser? user = await _applicationDbContext.Users.FindAsync(loggedInUser.Id);

            if (user == null)
            {
                _log.Error($"The user is null.");
                return false;
            }

            if (user.IsSuperAdmin)
            {
                user.LastAcceptedTermsVersion = _applicationInformation.TermsVersion;
                return true;
            }

            string? lastVersionAccepted = user.LastAcceptedTermsVersion;
            string? currentVersion = _applicationInformation.TermsVersion;

            return lastVersionAccepted == currentVersion;
        }

        /// <summary>
        /// Accepts the newest terms version
        /// </summary>
        /// <returns>A status code representing the status of the action</returns>
        public async Task<int> AcceptTermsAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Entities.ApplicationUser? user = await _applicationDbContext.Users.FindAsync(loggedInUser.Id);

            if (user == null)
            {
                _log.Error($"The user is null when trying to accept the terms.");
                return StatusCodes.Status400BadRequest;
            }

            user.LastAcceptedTermsVersion = _applicationInformation.TermsVersion;
            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }
    }
}
