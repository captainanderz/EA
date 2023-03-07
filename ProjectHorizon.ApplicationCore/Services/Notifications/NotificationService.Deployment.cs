using Microsoft.EntityFrameworkCore;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services.Notifications
{
    public partial class NotificationService
    {
        private const string successfulDeploymentMessage = "The deployment of '{0}', version {1} was successful.";
        private const string failedDeploymentMessage = "The deployment of '{0}', version {1} failed.";

        /// <summary>
        /// Generates the "SuccessfulDeployment" notification type
        /// </summary>
        /// <param name="application">The application that has a been successfully deployed</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repositoryThe id of the author of the action</param>
        /// <param name="authorId">The id of the author of the actionA bool that determines if the notification should be appear only for the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <param name="deployedVersion">The version of the application that was deployed</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulDeploymentNotificationsAsync(
            Application application,
            string version,
            Guid subscriptionId,
            bool isForPrivateRepository = false,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(successfulDeploymentMessage, application.Name, version);

            // Notify all users from the subscription
            subscriptionUsers = await _applicationDbContext
                .SubscriptionUsers
                .Where(su => su.SubscriptionId == subscriptionId)
                .Where(su => !su.ApplicationUser.NotificationSettings.Any(ns =>
                    ns.SubscriptionId == subscriptionId &&
                    ns.NotificationType == NotificationType.SuccessfulDeployment &&
                    !ns.IsEnabled))
                .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.SuccessfulDeployment,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = authorOnly,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }

        /// <summary>
        /// Generates the "FailedDeployment" notification type
        /// </summary>
        /// <param name="application">The application that tried to be deployed but failed</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <param name="deployedVersion">The version of the application that was deployed</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedDeploymentNotificationsAsync(
            Application? application,
            string version,
            Guid subscriptionId,
            bool isForPrivateRepository = false,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();

            string message = "";

            if (application is not null)
            {
                message = string.Format(failedDeploymentMessage, application.Name, version);
            }

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            Subscription? subscription = await _applicationDbContext.Subscriptions.FirstAsync(sub => sub.Id == subscriptionId);
            string subInfoMessage = $"Subscription: {subscription.Name}";

            List<ApplicationUser>? superAdmins = await _applicationDbContext
                .Users
                .Where(user => user.IsSuperAdmin)
                .ToListAsync();

            foreach (ApplicationUser? superAdmin in superAdmins)
            {
                _applicationDbContext.Notifications.Add(new()
                {
                    SubscriptionId = subscriptionId,
                    ApplicationUserId = superAdmin.Id,
                    Type = NotificationType.FailedDeployment,
                    Message = $"{message} {subInfoMessage}",
                });
            }

            subscriptionUsers = await _applicationDbContext
                .SubscriptionUsers
                .Where(su => su.SubscriptionId == subscriptionId)
                .Where(su => !su.ApplicationUser.NotificationSettings.Any(ns =>
                    ns.SubscriptionId == subscriptionId &&
                    ns.NotificationType == NotificationType.FailedDeployment &&
                    !ns.IsEnabled))
                .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.FailedDeployment,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = authorOnly,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }
    }
}
