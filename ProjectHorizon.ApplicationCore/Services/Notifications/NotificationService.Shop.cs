using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services.Notifications
{
    public partial class NotificationService
    {
        private const string applicationSuccessfullyAddedMessage = "The application '{0}' was successfully added to the shop.";
        private const string applicationFailedToAdd = "The application '{0}' has failed to be added to the shop.";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="authorId"></param>
        /// <param name="isForPrivateRepository"></param>
        /// <returns></returns>
        public async Task GenerateSuccessfulShopAddedNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(applicationSuccessfullyAddedMessage, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.Shop,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = true,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="applicationName"></param>
        /// <param name="authorId"></param>
        /// <param name="isForPrivateRepository"></param>
        /// <param name="extraErrorMessage"></param>
        /// <returns></returns>
        public async Task GenerateFailedShopAddedNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(applicationFailedToAdd, applicationName);

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.Shop,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = true,
                IsForPrivateRepository = isForPrivateRepository,
            };

            await GenerateNotificationsAsync(data);
        }
    }
}
