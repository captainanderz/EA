using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services.Notifications
{
    public partial class NotificationService
    {
        private const string successfulDeploymentScheduleAssignmentMessage = "The deployment schedule '{0}' has been successfully assigned.";
        private const string failedDeploymentScheduleAssignmentMessage = "The deployment schedule failed to assign.";

        private const string failedDeploymentScheduleAssignmentApply = "The deployment schedule '{0}' assign action failed to be applied in Microsoft Endpoint Manager. '{1}' might not be deployed.";
        private const string failedDeploymentScheduleClearApply = "The deployment schedule clear action failed to be applied in Microsoft Endpoint Manager. '{1}' might not be deployed.";

        private const string successfulDeploymentScheduleClearMessage = "The deployment schedule assigned to the application '{0}' has been successfully cleared.";
        private const string failedDeploymentScheduleClearMessage = "The deployment schedule profile failed to be cleared.";

        private const string startDeploymentScheduleMessage = "The deployment schedule {0} has started.";
        private const string failedStartDeploymentScheduleMessage = "The deployment schedule {0} failed to start";

        /// <summary>
        /// Generates the "SuccessfulDeploymentScheduleAssignment" notification type
        /// </summary>
        /// <param name="deploymentScheduleName">The name of the deployment schedule that is assigned</param>
        /// <param name="applicationName">The name of the application that is assigned</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulDeploymentScheduleAssignmentNotificationsAsync(
            string deploymentScheduleName,
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(successfulDeploymentScheduleAssignmentMessage, deploymentScheduleName, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "FailedDeploymentScheduleAssignment" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = failedDeploymentScheduleAssignmentMessage;

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "SuccessfulClearDeploymentSchedule" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that was cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulClearDeploymentScheduleNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(successfulDeploymentScheduleClearMessage, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "FailedClearDeploymentSchedule" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedClearDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "")
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = failedDeploymentScheduleClearMessage;

            if (!string.IsNullOrEmpty(extraErrorMessage))
            {
                message = $"{message} {extraErrorMessage}";
            }

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "FailedDeploymentScheduleAssignApply" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="deploymentScheduleName">The name of the deployment schedule that is assigned</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedDeploymentScheduleAssignApplyNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(failedDeploymentScheduleAssignmentApply, deploymentScheduleName, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "FailedDeploymentScheduleClearApply" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that failed to be cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedDeploymentScheduleClearApplyNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(failedDeploymentScheduleClearApply, applicationName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "SuccessfulStartDeploymentSchedule" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that failed to be cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateSuccessfulStartDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(startDeploymentScheduleMessage, deploymentScheduleName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
        /// Generates the "FailedStartDeploymentSchedule" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that failed to be cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        public async Task GenerateFailedStartDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string authorId,
            bool isForPrivateRepository = false)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(failedStartDeploymentScheduleMessage, deploymentScheduleName);

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.DeploymentSchedules,
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
