using AutoMapper;
using AutoMapper.QueryableExtensions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Properties;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services.Notifications
{
    public partial class NotificationService : INotificationService
    {
        private struct NotificationsData
        {
            public string NotificationType { get; set; }
            public Guid SubscriptionId { get; set; }
            public IEnumerable<SubscriptionUser> SubscriptionUsers { get; set; }
            public string Message { get; set; }
            public bool IsForPrivateRepository { get; set; }
            public string? AuthorId { get; set; }
            public bool AuthorOnly { get; set; }
        }

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IHubContext<SignalRHub> _messageHubContext;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly RecurringJobs _recurringJobsOptions;
        private readonly Options.Environment _envOptions;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        private readonly int numberOfRecentNotifications = 5;

        public NotificationService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            IHubContext<SignalRHub> messageHubContext,
            IEmailService emailService,
            IOptions<RecurringJobs> recurringJobs,
            IOptions<Options.Environment> envOptions,
            IAuditLogService auditLogService, 
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _messageHubContext = messageHubContext;
            _emailService = emailService;
            _recurringJobsOptions = recurringJobs.Value;
            _envOptions = envOptions.Value;
            _auditLogService = auditLogService;
            _loggedInUserProvider = loggedInUserProvider;
        }

        /// <summary>
        /// Gets all notifications and paginates them on the Notifications page
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns>A paged result with all notifications paged</returns>
        public async Task<PagedResult<NotificationDto>> ListNotificationsPagedAsync(
            UserDto loggedInUser,
            int pageNumber,
            int pageSize,
            string? searchTerm)
        {
            IQueryable<Notification>? queryNotifications = _applicationDbContext
                .Notifications
                .Where(n => n.ApplicationUserId == loggedInUser.Id);

            if (loggedInUser.UserRole != UserRole.SuperAdmin)
            {
                queryNotifications = queryNotifications.Where(n => n.SubscriptionId == loggedInUser.SubscriptionId);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryNotifications = queryNotifications.Where(n => n.Message.Contains(searchTerm.Trim()));
            }

            return new PagedResult<NotificationDto>
            {
                AllItemsCount = await queryNotifications.CountAsync(),
                PageItems = await queryNotifications
                    .OrderByDescending(n => n.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
                    .ToListAsync()
            };
        }

        /// <summary>
        /// Marks all notifications as read
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>Void</returns>
        public async Task MarkAllNotificationsAsReadAsync(UserDto loggedInUser)
        {
            string? query = "UPDATE Notifications SET IsRead = 1 WHERE ApplicationUserId = {0}";

            if (loggedInUser.UserRole != UserRole.SuperAdmin)
            {
                query += " AND SubscriptionId = {1}";
            }

            await _applicationDbContext.Database.ExecuteSqlRawAsync(query, loggedInUser.Id, loggedInUser.SubscriptionId);
        }

        /// <summary>
        /// Gets the last 5 recent notifications and show them in the bell-icon menu
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>An enumerable with the last 5 notifications</returns>
        public async Task<IEnumerable<NotificationDto>> GetRecentNotificationsAsync(UserDto loggedInUser)
        {
            IQueryable<Notification>? query = _applicationDbContext.Notifications
                .Where(n => n.ApplicationUserId == loggedInUser.Id);

            if (loggedInUser.UserRole != UserRole.SuperAdmin)
            {
                query = query.Where(n => n.SubscriptionId == loggedInUser.SubscriptionId);
            }

            return await query.OrderByDescending(n => n.CreatedOn)
                .Take(numberOfRecentNotifications)
                .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the unread notifications older than one day
        /// </summary>
        /// <returns>An enumerable with all unread notifications older than one day</returns>
        public async Task<IEnumerable<SubscriptionUser>> GetDataForUnreadNotificationsEmail()
        {
            List<SubscriptionUser>? subscriptionUsersWithNotifications = await _applicationDbContext
                .SubscriptionUsers
                .Where(su => !su.ApplicationUser.IsSuperAdmin && su.ApplicationUser.Notifications.Any())
                .Include(su => su.Subscription)
                .Include(su => su.ApplicationUser.Notifications
                    .Where(n => !n.IsRead && n.ModifiedOn > DateTime.UtcNow.AddDays(-1)))
                .ToListAsync();

            return subscriptionUsersWithNotifications;
        }

        /// <summary>
        /// Sends an email to users with the notifications older than a day that they didn't read
        /// </summary>
        /// <returns>Void</returns>
        public async Task SendUnreadNotificationEmailsAsync()
        {
            if (!_recurringJobsOptions.SendUnreadNotificationEmails)
            {
                return;
            }

            List<SubscriptionUser>? subscriptionUsersWithNotifications = await _applicationDbContext
                .SubscriptionUsers
                .Where(su => !su.ApplicationUser.IsSuperAdmin && su.ApplicationUser.Notifications.Any())
                .Include(su => su.Subscription)
                .Include(su => su.ApplicationUser.Notifications
                    .Where(n => !n.IsRead && n.ModifiedOn > DateTime.UtcNow.AddDays(-1)))
                .ToListAsync();

            foreach (SubscriptionUser? userWithNotifications in await GetDataForUnreadNotificationsEmail())
            {
                (string htmlContent, List<Attachment> attachments) = BuildUnreadNotificationsEmail(userWithNotifications);

                if (htmlContent != null)
                {
                    await _emailService.SendEmailAsync(new EmailDetailsDto
                    {
                        ToEmail = userWithNotifications.ApplicationUser.Email,
                        ToName = userWithNotifications.ApplicationUser.FullName,
                        Subject = $"You have unread notifications - {userWithNotifications.Subscription.Name}",
                        HTMLContent = htmlContent,
                        Attachments = attachments
                    });
                }
            }
        }

        /// <summary>
        /// Gives the Admin (or SuperAdmin) of his subscription the option to enable or disable notification types for the users with lesser role in his subscription
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <returns>An enumerable containing the notification settings the Admin/SuperAdmin has made for a subscription</returns>
        public async Task<IEnumerable<UserNotificationSettingDto>> FindUsersNotificationSettingsBySubscriptionAsync(
            UserDto loggedInUser)
        {
            IEnumerable<UserNotificationSettingDto> usersNotificationSettings;

            IQueryable<SubscriptionUser>? query = _applicationDbContext
                .SubscriptionUsers
                .Where(subUser => subUser.SubscriptionId == loggedInUser.SubscriptionId);

            if (loggedInUser.UserRole != UserRole.SuperAdmin && loggedInUser.UserRole != UserRole.Administrator)
            {
                query = query.Where(subUser => subUser.ApplicationUserId == loggedInUser.Id);
            }

            usersNotificationSettings = await query
                .OrderBy(su => su.ApplicationUser.FirstName)
                .ThenBy(su => su.ApplicationUser.LastName)
                .Select(subUser => new UserNotificationSettingDto
                {
                    UserId = subUser.ApplicationUserId,
                    FullName = subUser.ApplicationUser.FullName,
                    ProfilePictureSmall = subUser.ApplicationUser.ProfilePictureSmall
                })
                .ToListAsync();

            await _applicationDbContext
                .NotificationSettings
                .Where(notificationSetting =>
                    notificationSetting.SubscriptionId == loggedInUser.SubscriptionId &&
                    !notificationSetting.IsEnabled)
                .ForEachAsync(notificationSetting =>
                {
                    UserNotificationSettingDto? userNotificationSetting = usersNotificationSettings
                        .SingleOrDefault(userNotificationSetting => userNotificationSetting.UserId == notificationSetting.ApplicationUserId);

                    if (userNotificationSetting != null)
                    {
                        switch (notificationSetting.NotificationType)
                        {
                            case NotificationType.NewApplication:
                                userNotificationSetting.IsNewApplication = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.NewVersion:
                                userNotificationSetting.IsNewVersion = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.ManualApproval:
                                userNotificationSetting.IsManualApproval = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.SuccessfulDeployment:
                                userNotificationSetting.IsSuccessfulDeployment = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.FailedDeployment:
                                userNotificationSetting.IsFailedDeployment = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.DeletedApplication:
                                userNotificationSetting.IsDeletedApplication = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.Shop:
                                userNotificationSetting.IsShop = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.AssignmentProfiles:
                                userNotificationSetting.IsAssignmentProfiles = notificationSetting.IsEnabled;
                                break;

                            case NotificationType.DeploymentSchedules:
                                userNotificationSetting.IsDeploymentSchedules = notificationSetting.IsEnabled;
                                break;
                        }
                    }
                });

            return usersNotificationSettings;
        }

        /// <summary>
        /// Updates a notification setting
        /// </summary>
        /// <param name="notificationSettingDto">The notification setting dto that will be updated</param>
        /// <returns>The notification settings dto that was updated</returns>
        public async Task<NotificationSettingDto> UpdateNotificationSettingAsync(NotificationSettingDto notificationSettingDto)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            NotificationSetting? notificationSetting = await _applicationDbContext
                .NotificationSettings
                .SingleOrDefaultAsync(notificationSetting =>
                    notificationSetting.SubscriptionId == notificationSettingDto.SubscriptionId &&
                    notificationSetting.ApplicationUserId == notificationSettingDto.ApplicationUserId &&
                    notificationSetting.NotificationType == notificationSettingDto.NotificationType);

            string auditLogText = notificationSettingDto.IsEnabled ? "enabled" : "disabled";

            if (notificationSetting != null)
            {
                notificationSetting.IsEnabled = notificationSettingDto.IsEnabled;
            }
            else
            {
                _applicationDbContext
                    .NotificationSettings
                    .Add(_mapper.Map<NotificationSetting>(notificationSettingDto));
            }

            ApplicationUser? appUser = await _applicationDbContext.Users
                .SingleOrDefaultAsync(user => user.Id == notificationSettingDto.ApplicationUserId);

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.NotificationSettings,
                string.Format(AuditLogActions.NotificationSetting, auditLogText, notificationSettingDto.NotificationType, appUser?.FullName),
                author: loggedInUser,
                saveChanges: false);

            await _applicationDbContext.SaveChangesAsync();

            return notificationSettingDto;
        }

        /// <summary>
        /// Update multiple notification settings
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="bulkNotificationSettingsDto">The bulk notification settings dto that will be updated</param>
        /// <returns>Void</returns>
        public async Task UpdateBulkNotificationSettingsAsync(
            Guid subscriptionId, BulkNotificationSettingsDto bulkNotificationSettingsDto)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            if (bulkNotificationSettingsDto.IsEnabled)
            {
                await _applicationDbContext
                    .NotificationSettings
                    .Where(notificationSetting =>
                        notificationSetting.SubscriptionId == subscriptionId &&
                        bulkNotificationSettingsDto.UserIds.Contains(notificationSetting.ApplicationUserId) &&
                        !notificationSetting.IsEnabled)
                    .ForEachAsync(notificationSetting => notificationSetting.IsEnabled = bulkNotificationSettingsDto.IsEnabled);
            }
            else
            {
                List<NotificationSetting>? notificationSettings = await _applicationDbContext
                    .NotificationSettings
                    .Where(notificationSetting =>
                        notificationSetting.SubscriptionId == subscriptionId &&
                        bulkNotificationSettingsDto.UserIds.Contains(notificationSetting.ApplicationUserId))
                    .ToListAsync();

                foreach (string? userId in bulkNotificationSettingsDto.UserIds)
                {
                    foreach (string? notificationType in NotificationType.Values)
                    {
                        NotificationSetting? notificationSetting = notificationSettings.SingleOrDefault(notificationSetting =>
                            notificationSetting.NotificationType == notificationType &&
                            notificationSetting.ApplicationUserId == userId);

                        if (notificationSetting != null)
                        {
                            notificationSetting.IsEnabled = bulkNotificationSettingsDto.IsEnabled;
                        }
                        else
                        {
                            _applicationDbContext.NotificationSettings.Add(new NotificationSetting
                            {
                                SubscriptionId = subscriptionId,
                                ApplicationUserId = userId,
                                NotificationType = notificationType,
                                IsEnabled = bulkNotificationSettingsDto.IsEnabled
                            });
                        }
                    }
                }
            }

            string bulkAction = bulkNotificationSettingsDto.IsEnabled ? "enabled " : "disabled";

            List<string> userNames = await _applicationDbContext.Users
                .Where(user => bulkNotificationSettingsDto.UserIds.Contains(user.Id))
                .Select(user => user.FullName)
                .ToListAsync();

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.NotificationSettings,
                string.Format(AuditLogActions.NotificationSettingsBulk, bulkAction, string.Join(", ", userNames)),
                saveChanges: false,
                author: loggedInUser);

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data">The data you want to transmit through the notification</param>
        /// <returns>Void</returns>
        private async Task GenerateNotificationsAsync(NotificationsData data)
        {
            var userIds = data.SubscriptionUsers.Select(user => user.ApplicationUserId).ToList();

            if (!data.AuthorOnly)
            {
                // Generate notifications for the users from above
                foreach (SubscriptionUser? subscriptionUser in data.SubscriptionUsers)
                {
                    _applicationDbContext.Notifications.Add(new()
                    {
                        SubscriptionId = subscriptionUser.SubscriptionId,
                        ApplicationUserId = subscriptionUser.ApplicationUserId,
                        Type = data.NotificationType,
                        Message = data.Message,
                        ForPrivateRepository = data.IsForPrivateRepository,
                    });
                }
            }

            // Notified the superadmin (author of the notification)
            if (!string.IsNullOrEmpty(data.AuthorId) && !data.SubscriptionUsers.Any(user => user.ApplicationUserId == data.AuthorId))
            {
                NotificationSetting? setting = await _applicationDbContext
                    .NotificationSettings
                    .Where(s => s.ApplicationUserId == data.AuthorId && s.NotificationType == data.NotificationType)
                    .FirstOrDefaultAsync();

                if (setting is null || setting.IsEnabled)
                {
                    _applicationDbContext.Notifications.Add(new()
                    {
                        SubscriptionId = data.SubscriptionId,
                        ApplicationUserId = data.AuthorId,
                        Type = data.NotificationType,
                        Message = data.Message,
                        ForPrivateRepository = data.IsForPrivateRepository,
                    });

                    userIds.Add(data.AuthorId);
                }
            }

            await _applicationDbContext.SaveChangesAsync();

            await _messageHubContext.Clients.Users(userIds).SendAsync(SignalRMessages.UserNotification);
        }

        /// <summary>
        /// Builds the email content and template for the unread notifications older than one day
        /// </summary>
        /// <param name="subscriptionUser">The user that is part of the subscription</param>
        /// <returns></returns>
        private (string, List<Attachment>) BuildUnreadNotificationsEmail(SubscriptionUser subscriptionUser)
        {
            string? emailTemplate = EmailResources.DailyNotificationsTemplate;
            emailTemplate = emailTemplate.Replace("#endpointAdminUrl", _envOptions.BaseUrl);
            emailTemplate = emailTemplate.Replace("#logoUrl", _envOptions.BaseUrl);

            List<Attachment> attachments = new List<Attachment>
            {
                ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo))
            };

            HtmlDocument? htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(emailTemplate);
            HtmlNode? htmlBody = htmlDoc.DocumentNode;

            bool hasNotifications = false;

            foreach (string? notificationType in NotificationType.Values)
            {
                if (UpdateHtmlTemplate(subscriptionUser, htmlBody, notificationType))
                {
                    hasNotifications = true;
                    attachments.Add(AttachmentFromNotificationType(notificationType));
                }
            }

            return hasNotifications ? (htmlBody.InnerHtml, attachments) : (null, null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="notificationType">The notification type of the notification that is unread</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static Attachment AttachmentFromNotificationType(string notificationType) =>
            notificationType switch
            {
                NotificationType.NewApplication => ApplicationHelper.GetImageAttachment(EmailResources.NewAppIcon, ImageFormat.Png, nameof(EmailResources.NewAppIcon)),
                NotificationType.NewVersion => ApplicationHelper.GetImageAttachment(EmailResources.NewVersionIcon, ImageFormat.Png, nameof(EmailResources.NewVersionIcon)),
                NotificationType.ManualApproval => ApplicationHelper.GetImageAttachment(EmailResources.ApprovalsIcon, ImageFormat.Png, nameof(EmailResources.ApprovalsIcon)),
                NotificationType.SuccessfulDeployment => ApplicationHelper.GetImageAttachment(EmailResources.SuccessIcon, ImageFormat.Png, nameof(EmailResources.SuccessIcon)),
                NotificationType.FailedDeployment => ApplicationHelper.GetImageAttachment(EmailResources.FailedIcon, ImageFormat.Png, nameof(EmailResources.FailedIcon)),
                _ => throw new InvalidOperationException(),
            };

        /// <summary>
        /// Updates the HTML template of the email about unread notifications
        /// </summary>
        /// <param name="subscriptionUser">The user that is part of the subscription</param>
        /// <param name="htmlBody">The HTML content of the email</param>
        /// <param name="notificationType">The notification type of the notification that is unread</param>
        /// <returns></returns>
        private static bool UpdateHtmlTemplate(SubscriptionUser subscriptionUser, HtmlNode htmlBody, string notificationType)
        {
            bool disabledNotficationType = subscriptionUser
                .ApplicationUser
                .NotificationSettings.Any(ns => ns.NotificationType == notificationType && !ns.IsEnabled);

            if (disabledNotficationType)
            {
                return false;
            }

            string nodeText = @"<tr><td><a href=""#"" style=""text-decoration: none; color: #989898"">{0}</a></td></tr>";

            List<Notification>? notifications = subscriptionUser
                .ApplicationUser
                .Notifications
                .Where(n => n.Type == notificationType && n.SubscriptionId == subscriptionUser.SubscriptionId)
                .ToList();

            bool hasNotifications = notifications != null && notifications.Any();

            if (hasNotifications)
            {
                string? query = $"//table[@id='{notificationType}Table']";
                HtmlNode? node = htmlBody.SelectSingleNode(query);

                if (node is null)
                {
                    return false;
                }

                foreach (Notification? notification in notifications)
                {
                    HtmlNode htmlNode = HtmlNode.CreateNode(string.Format(nodeText, $"{notification.ModifiedOn:dd/MM/yyyy HH:mm} - {notification.Message}"));
                    node.AppendChild(htmlNode);
                }

                return true;
            }

            string? removeQuery = $"//table[@id='{notificationType}Card']";
            HtmlNode? removeNode = htmlBody.SelectSingleNode(removeQuery);

            if (removeNode is not null)
            {
                removeNode.Remove();
            }

            return false;
        }
    }
}