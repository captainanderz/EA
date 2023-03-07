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
        private const string approvalMessage = "Manual approval needed to update '{0}' from version {1} to version {2}.";

        /// <summary>
        /// Generates the "ManualApproval" notification type
        /// </summary>
        /// <param name="application">The application that has the "Manual Approve" option set to true</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="authorId">The id of the author of the action</param>
        /// <param name="authorOnly">A bool that determines if the notification should appear only for the author of the action</param>
        /// <param name="deployedVersion">The version of the application that was deployed</param>
        /// <returns>Void</returns>
        public async Task GenerateManualApprovalNotificationsAsync(
            Application application,
            Guid subscriptionId,
            string? authorId = null,
            bool authorOnly = false,
            string? deployedVersion = null)
        {
            IEnumerable<SubscriptionUser> subscriptionUsers = new List<SubscriptionUser>();
            string message = string.Format(approvalMessage, application.Name, deployedVersion, application.Version);

            subscriptionUsers = await _applicationDbContext
                .SubscriptionUsers
                .Where(su => su.SubscriptionId == subscriptionId)
                .Where(su => !su.ApplicationUser.NotificationSettings.Any(ns =>
                    ns.SubscriptionId == subscriptionId &&
                    ns.NotificationType == NotificationType.ManualApproval &&
                    !ns.IsEnabled))
                .ToListAsync();

            NotificationsData data = new NotificationsData
            {
                NotificationType = NotificationType.ManualApproval,
                SubscriptionId = subscriptionId,
                SubscriptionUsers = subscriptionUsers,
                Message = message,
                AuthorId = authorId,
                AuthorOnly = authorOnly,
                IsForPrivateRepository = false,
            };

            await GenerateNotificationsAsync(data);
        }
    }
}
