using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Gets all notifications and paginates them on the Notifications page
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns>A list with all notifications paged</returns>
        Task<PagedResult<NotificationDto>> ListNotificationsPagedAsync(
            UserDto loggedInUser,
            int pageNumber,
            int pageSize,
            string? searchTerm);

        /// <summary>
        /// Marks all notifications as read
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>Void</returns>
        Task MarkAllNotificationsAsReadAsync(UserDto loggedInUser);

        /// <summary>
        /// Gets the last 5 recent notifications and show them in the bell-icon menu
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>An enumerable with the last 5 notifications</returns>
        Task<IEnumerable<NotificationDto>> GetRecentNotificationsAsync(UserDto loggedInUser);

        /// <summary>
        /// Gets the unread notifications older than one day
        /// </summary>
        /// <returns>An enumerable with all unread notifications older than one day</returns>
        Task<IEnumerable<SubscriptionUser>> GetDataForUnreadNotificationsEmail();

        /// <summary>
        /// Sends an email to users with the notifications older than a day that they didn't read
        /// </summary>
        /// <returns>Void</returns>
        Task SendUnreadNotificationEmailsAsync();

        /// <summary>
        /// Gives the Admin (or SuperAdmin) of his subscription the option to enable or disable notification types for the users with lesser role in his subscription
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>An enumerable containing the notification settings the Admin/SuperAdmin has made for a subscription</returns>
        Task<IEnumerable<UserNotificationSettingDto>> FindUsersNotificationSettingsBySubscriptionAsync(UserDto loggedInUser);

        /// <summary>
        /// Updates a notification setting
        /// </summary>
        /// <param name="notificationSettingDto">The notification setting dto that will be updated</param>
        /// <returns>The notification settings dto that was updated</returns>
        Task<NotificationSettingDto> UpdateNotificationSettingAsync(NotificationSettingDto notificationSettingDto);

        /// <summary>
        /// Update multiple notification settings
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="bulkNotificationSettingsDto">The bulk notification settings dto that will be updated</param>
        /// <returns>Void</returns>
        Task UpdateBulkNotificationSettingsAsync(Guid subscriptionId, BulkNotificationSettingsDto bulkNotificationSettingsDto);

        /// <summary>
        /// Generates the "NewApplication" notification type
        /// </summary>
        /// <param name="application">The new application that was just added</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <returns>Void</returns>
        Task GenerateNewApplicationNotificationsAsync(
           Application application,
           Guid subscriptionId,
           bool isForPrivateRepository,
           string? authorId = null);

        /// <summary>
        /// Generates the "DeleteApplication" notification type
        /// </summary>
        /// <param name="application">The application we want to delete</param>
        /// <param name="subscriptionId">The id of the subscription</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <returns>Void</returns>
        Task GenerateDeleteApplicationNotificationsAsync(
            Application application,
            Guid subscriptionId,
            bool isForPrivateRepository,
            string? authorId = null);

        /// <summary>
        /// Generates the "NewVersion" notification type
        /// </summary>
        /// <param name="application">The application that has a new version uploaded</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <returns>Void</returns>
        Task GenerateNewVersionNotificationsAsync(
            Application application,
            Guid subscriptionId,
            string? authorId = null,
            bool authorOnly = false);

        /// <summary>
        /// Generates the "ManualApproval" notification type
        /// </summary>
        /// <param name="application">The application that has the "Manual Approve" option set to true</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <param name="deployedVersion">The version of the application that was deployed</param>
        /// <returns>Void</returns>
        Task GenerateManualApprovalNotificationsAsync(
            Application application,
            Guid subscriptionId,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null);

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
        Task GenerateSuccessfulDeploymentNotificationsAsync(
            Application application,
            string version,
            Guid subscriptionId,
            bool isForPrivateRepository = false,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null);

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
        Task GenerateFailedDeploymentNotificationsAsync(
            Application application,
            string version,
            Guid subscriptionId,
            bool isForPrivateRepository = false,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "SuccessfulAssignmentProfileAssignment" notification type
        /// </summary>
        /// <param name="assignmentProfileName">The name of the assignment profile that is assigned</param>
        /// <param name="applicationName">The name of the application that the assignment profile is assigned to</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateSuccessfulAssignmentProfileAssignmentNotificationsAsync(
            string assignmentProfileName,
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedAssignmentProfileAssignment" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        Task GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
           Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "SuccessfulClearAssignmentProfile" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that the assignment profile was cleared from</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateSuccessfulClearAssignmentProfileNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedClearAssignmentProfile" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        Task GenerateFailedClearAssignmentProfileNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "FailedAssignmentProfileAssignApply" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateFailedAssignmentProfileAssignApplyNotificationsAsync(
            Guid subscriptionId,
            string assignmentProfileName,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedAssignmentProfileClearApply" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application from which the assignment profile clear action tried to apply</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        /// <exception cref="NotImplementedException"></exception>
        Task GenerateFailedAssignmentProfileClearApplyNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "NewApplicationWarningNotifications" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationName">The name of the new application that is generating warnings</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateNewApplicationWarningNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId);

        /// <summary>
        /// Generates the "SuccessfulAddToShop" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that we try to add in the shop</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns></returns>
        Task GenerateSuccessfulShopAddedNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedAddToShop" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationName">The name of the application that we try to add in the shop</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns></returns>
        Task GenerateFailedShopAddedNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "SuccessfulDeploymentScheduleAssignment" notification type
        /// </summary>
        /// <param name="deploymentScheduleName">The name of the deployment schedule that is assigned</param>
        /// <param name="applicationName">The name of the application that is assigned</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateSuccessfulDeploymentScheduleAssignmentNotificationsAsync(
            string deploymentScheduleName,
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedDeploymentScheduleAssignment" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        Task GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "SuccessfulClearDeploymentSchedule" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that was cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateSuccessfulClearDeploymentScheduleNotificationsAsync(
            string applicationName,
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedClearDeploymentSchedule" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <param name="extraErrorMessage">A string that represent a message that is added at the end of the default error message for additional information in edge cases</param>
        /// <returns>Void</returns>
        Task GenerateFailedClearDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string authorId,
            bool isForPrivateRepository = false,
            string extraErrorMessage = "");

        /// <summary>
        /// Generates the "FailedDeploymentScheduleAssignApply" notification type
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="deploymentScheduleName">The name of the deployment schedule that is assigned</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateFailedDeploymentScheduleAssignApplyNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false);

        /// <summary>
        /// Generates the "FailedDeploymentScheduleClearApply" notification type
        /// </summary>
        /// <param name="applicationName">The name of the application that failed to be cleared</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="isForPrivateRepository">A bool that determines if the application is in private or public repository</param>
        /// <returns>Void</returns>
        Task GenerateFailedDeploymentScheduleClearApplyNotificationsAsync(
            Guid subscriptionId,
            string applicationName,
            string authorId,
            bool isForPrivateRepository = false);

        ///
        Task GenerateSuccessfulStartDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string authorId,
            bool isForPrivateRepository = false);

        ///
        Task GenerateFailedStartDeploymentScheduleNotificationsAsync(
            Guid subscriptionId,
            string deploymentScheduleName,
            string authorId,
            bool isForPrivateRepository = false);
    }
}
