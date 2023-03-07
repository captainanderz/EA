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
        private const string newApplicationMessage = "A new application, '{0}', was added to the {1} repository.";
        private const string newApplicationWarningMessage = "Microsoft Endpoint Manager conversion for public application '{0}' was not successful.";
        private const string newVersionMessage = "A new version ({0}) for '{1}' is available in the {2} repository";
        private const string deleteApplicationMessage = "The application '{0}', was deleted from the {1} repository.";

        /// <summary>
        /// Generates the "NewApplication" notification type
        /// </summary>
        /// <param name="application">The new application that was just added</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <returns>Void</returns>
        public async Task GenerateNewApplicationNotificationsAsync(
            Application application,
            Guid subscriptionId,
            bool isForPrivateRepository,
            string? authorId = null)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string? repositoryType = isForPrivateRepository ? RepositoryType.Private.ToLower() : RepositoryType.Public.ToLower();
            string message = string.Format(newApplicationMessage, application.Name, repositoryType);

            // If the user has disabled notifications then he is not in the list of the subscription users who would get notified
            subscriptionUsers = await _applicationDbContext
                   .SubscriptionUsers
                   .Where(su => !su.ApplicationUser.NotificationSettings.Any(ns =>
                       ns.SubscriptionId == su.SubscriptionId &&
                       ns.NotificationType == NotificationType.NewApplication &&
                       !ns.IsEnabled))
                   .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.NewApplication,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = false,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }

        /// <summary>
        /// Generates the "NewVersion" notification type
        /// </summary>
        /// <param name="application">The application that has a new version uploaded</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <returns>Void</returns>
        public async Task GenerateNewVersionNotificationsAsync(
            Application application,
            Guid subscriptionId,
            string? authorId = null,
            bool authorOnly = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string repositoryType = RepositoryType.Public.ToLower();
            string message = string.Empty;

            if (authorOnly)
            {
                message = string.Format(newApplicationWarningMessage, application.Name);
            }
            else
            {
                message = string.Format(newVersionMessage, application.Version, application.Name, repositoryType);
            }

            // New version notifications needs to be sent only to users who has the application deployed in their subscription
            subscriptionUsers = await _applicationDbContext
                   .SubscriptionUsers
                   .Join(
                       _applicationDbContext.SubscriptionPublicApplications,
                       su => su.SubscriptionId,
                       spa => spa.SubscriptionId,
                       (su, spa) => new { SubscriptionUser = su, spa.PublicApplicationId })
                   .Where(x => x.PublicApplicationId == application.Id)
                   .Where(x => !x.SubscriptionUser.ApplicationUser.NotificationSettings.Any(ns =>
                       ns.SubscriptionId == x.SubscriptionUser.SubscriptionId &&
                       ns.NotificationType == NotificationType.NewVersion &&
                       !ns.IsEnabled))
                   .Select(x => x.SubscriptionUser)
                   .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.NewVersion,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = authorOnly,
                IsForPrivateRepository = false,
            };

            await GenerateNotificationsAsync(data);
        }

        /// <summary>
        /// Generates the "NewApplicationWarningNotifications" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationName">The name of the new application that is generating warnings</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <returns>Void</returns>
        public async Task GenerateNewApplicationWarningNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(newApplicationWarningMessage, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = true,
                IsForPrivateRepository = false,
            };

            await GenerateNotificationsAsync(data);
        }

        /// <summary>
        /// Generates the "DeleteApplication" notification type
        /// </summary>
        /// <param name="application">The application we want to delete</param>
        /// <param name="subscriptionId">The id of the subscription</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns></returns>
        public async Task GenerateDeleteApplicationNotificationsAsync(
            Application application,
            Guid subscriptionId,
            bool isForPrivateRepository,
            string? authorId = null)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string? repositoryType = isForPrivateRepository ? RepositoryType.Private.ToLower() : RepositoryType.Public.ToLower();
            string message = string.Format(deleteApplicationMessage, application.Name, repositoryType);

            // If the user has disabled notifications then he is not in the list of the subscription users who would get notified

            subscriptionUsers = await _applicationDbContext
                   .SubscriptionUsers
                   .Where(su => !su.ApplicationUser.NotificationSettings.Any(ns =>
                       ns.SubscriptionId == su.SubscriptionId &&
                       ns.NotificationType == NotificationType.DeletedApplication &&
                       !ns.IsEnabled))
                   .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeletedApplication,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = false,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }
    }
}
