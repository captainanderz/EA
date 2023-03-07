using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services.Notifications
{
    public partial class NotificationService
    {
        private const string successfulAssignmentProfileAssignmentMessage = "The assignment profile '{0}' has been successfully assigned.";
        private const string failedAssignmentProfileAssignmentMessage = "The assignment profile failed to assign.";

        private const string failedAssignmentProfileAssignmentApply = "The assignment profile '{0}' assign action failed to be applied in Microsoft Endpoint Manager. '{1}' might not be deployed.";
        private const string failedAssignmentProfileClearApply = "The assignment profile clear action failed to be applied in Microsoft Endpoint Manager. '{0}' might not be deployed.";

        private const string successfulAssignmentProfileClearMessage = "The assignment profile assigned to the application '{0}' has been successfully cleared from Endpoint Admin and Microsoft Endpoint Manager.";
        private const string failedAssignmentProfileClearMessage = "The assigned assignment profile failed to be cleared.";

        /// <summary>
        /// Generates the "SuccessfulAssignmentProfileAssignment" notification type
        /// </summary>
        /// <param name="assignmentProfileName">The name of the assignment profile that is assigned</param>
        /// <param name="applicationName">The name of the application that the assignment profile is assigned to</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulAssignmentProfileAssignmentNotificationsAsync(
            string assignmentProfileName,
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(successfulAssignmentProfileAssignmentMessage, assignmentProfileName, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
        /// Generates the "FailedAssignmentProfileAssignment" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = failedAssignmentProfileAssignmentMessage;

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
        /// Generates the "SuccessfulClearAssignmentProfile" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that the assignment profile was cleared from</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulClearAssignmentProfileNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(successfulAssignmentProfileClearMessage, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
        /// Generates the "FailedClearAssignmentProfile" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedClearAssignmentProfileNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = failedAssignmentProfileClearMessage;

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
        /// Generates the "FailedAssignmentProfileAssignApply" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedAssignmentProfileAssignApplyNotificationsAsync(
            Guid subscriptionId,
            string assignmentProfileName,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(failedAssignmentProfileAssignmentApply, assignmentProfileName, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
        /// Generates the "FailedAssignmentProfileClearApply" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application from which the assignment profile clear action tried to apply</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task GenerateFailedAssignmentProfileClearApplyNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(failedAssignmentProfileClearApply, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.AssignmentProfiles,
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
