using Hangfire.Server;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.DTOs;
using System.Threading.Tasks;
using System;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IDeployIntunewinService
    {
        Task DeployPublicApplicationForSubscriptionAsync(UserDto loggedInUser, Guid subscriptionId, int applicationId, PerformContext context, bool readRequirementScript);

        Task DeployPublicApplicationVersionForSubscriptionAsync(UserDto loggedInUser, Guid subscriptionId, int applicationId, string version, PerformContext context, bool readRequirementScript);

        Task DeployPrivateApplicationForSubscriptionAsync(UserDto loggedInUser, int applicationId, PerformContext context, bool readRequirementScript);

        Task DeployPrivateApplicationVersionForSubscriptionAsync(UserDto loggedInUser, int applicationId, string version, PerformContext context, bool readRequirementScript);

        Task RemoveDeployedPublicApplicationAsync(int applicationId);

        Task<MobileLobApp> GetDeployedPublicApplicationInfoAsync(int applicationId);

        Task UpdateDevicesCountAsync();

        Task UpdateDevicesCountForAllSubscriptionsAsync();

        Task TogglePrivateRequirementScriptAsync(Guid subscriptionId, int applicationId, string intuneId, bool useRequirementScript);

        Task TogglePublicRequirementScriptAsync(Guid subscriptionId, int applicationId, string intuneId, bool useRequirementScript);

        Task<string> PublishPrivateAppToIntuneAsNewAsync(
            Guid subscriptionId,
            int applicationId);

        Task<string> PublishPublicAppToIntuneAsNewAsync(
            Guid subscriptionId,
            int applicationId);

        Task RemoveDeployedApplicationAsync(Guid subscribtionId, string intuneId);
    }
}
