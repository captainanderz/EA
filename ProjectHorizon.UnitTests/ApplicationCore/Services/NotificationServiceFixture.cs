using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class NotificationServiceFixture : DbServiceFixture
    {
        private static readonly Guid _validSubscriptionIdForNotificationSetting = Guid.NewGuid();
        private static readonly Guid _validSubscriptionIdForNotification = Guid.NewGuid();

        private static readonly string _validSuperAdminUserId = Guid.NewGuid().ToString();
        private static readonly string _validAdministratorUserId = Guid.NewGuid().ToString();
        private static readonly string _validContributorUserId = Guid.NewGuid().ToString();
        private static readonly string _validReaderUserId = Guid.NewGuid().ToString();

        public override async Task InitializeAsync()
        {
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();
            UserManager<ApplicationUser>? userManager = Services.GetRequiredService<UserManager<ApplicationUser>>();

            Subscription? subscriptionForNotification = new Subscription
            {
                Id = _validSubscriptionIdForNotification,
                Name = "Zwable-Subscription-Notification",
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            Subscription? subscriptionForNotificationSettings = new Subscription
            {
                Id = _validSubscriptionIdForNotificationSetting,
                Name = "Zwable-Subscription-NotificationSettings",
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            context.Subscriptions.AddRange(subscriptionForNotification, subscriptionForNotificationSettings);

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validSuperAdminUserId,
                UserName = "SuperAdmin",
                FirstName = "Test-User-SuperAdmin first name",
                LastName = "Test-User-SuperAdmin last name",
                IsSuperAdmin = true,
            });

            ApplicationUser? administratorUser = new ApplicationUser
            {
                Id = _validAdministratorUserId,
                UserName = "Admin",
                FirstName = "Test-User-Administrator first name",
                LastName = "Test-User-Administrator last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        UserRole = UserRole.Administrator,
                        Subscription = subscriptionForNotificationSettings
                    }
                }
            };
            await userManager.CreateAsync(administratorUser);

            ApplicationUser? contributorUser = new ApplicationUser
            {
                Id = _validContributorUserId,
                UserName = "Contributor",
                FirstName = "Test-User-Contributor first name",
                LastName = "Test-User-Contributor last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        UserRole = UserRole.Contributor,
                        Subscription = subscriptionForNotificationSettings
                    }
                }
            };
            await userManager.CreateAsync(contributorUser);

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validReaderUserId,
                UserName = "Reader",
                FirstName = "Test-User-Reader first name",
                LastName = "Test-User-Reader last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        UserRole = UserRole.Reader,
                        Subscription = subscriptionForNotificationSettings
                    }
                }
            });

            context.NotificationSettings.AddRange(new NotificationSetting
            {
                NotificationType = NotificationType.NewApplication,
                IsEnabled = false,
                Subscription = subscriptionForNotificationSettings,
                ApplicationUser = administratorUser
            },
                new NotificationSetting
                {
                    NotificationType = NotificationType.NewVersion,
                    IsEnabled = true,
                    Subscription = subscriptionForNotificationSettings,
                    ApplicationUser = contributorUser
                });

            context.Notifications.AddRange(new Notification
            {
                IsRead = false,
                Type = NotificationType.NewVersion,
                Message = "UnReadNotificationAdmin",
                Subscription = subscriptionForNotification,
                ApplicationUser = administratorUser
            }, new Notification
            {
                IsRead = true,
                Type = NotificationType.NewVersion,
                Message = "ReadNotificationAdmin",
                Subscription = subscriptionForNotification,
                ApplicationUser = administratorUser
            },
                new Notification
                {
                    IsRead = false,
                    Type = NotificationType.NewVersion,
                    Message = "UnReadNotificationContributor",
                    Subscription = subscriptionForNotification,
                    ApplicationUser = contributorUser
                }, new Notification
                {
                    IsRead = true,
                    Type = NotificationType.NewVersion,
                    Message = "ReadNotificationContributor",
                    Subscription = subscriptionForNotification,
                    ApplicationUser = contributorUser
                });

            await context.SaveChangesAsync();
        }

        public static readonly IEnumerable<object[]> UpdateNotificationSettingAsyncData = new List<object[]>
        {
            new object[]
            {
                new NotificationSettingDto
                {
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    ApplicationUserId = _validAdministratorUserId,
                    NotificationType = NotificationType.NewApplication,
                    IsEnabled = true
                },
                new UserDto
                {
                    Id = _validAdministratorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                true, //the same value as IsEnabled above
            },
            new object[]
            {
                new NotificationSettingDto
                {
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    ApplicationUserId = _validAdministratorUserId,
                    NotificationType = NotificationType.NewVersion,
                    IsEnabled = true
                },
                new UserDto
                {
                    Id = _validAdministratorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                true,
            },
        };

        public static readonly IEnumerable<object[]> FindUsersNotificationSettingsBySubscriptionAsyncData = new List<object[]>
        {
            new object[]
            {
                new UserDto
                {
                    Id = _validSuperAdminUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.SuperAdmin,
                    SourceIP = "localhost"
                },
                3, //number of userNotificationSettings found
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validAdministratorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                3,
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validContributorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Contributor,
                    SourceIP = "localhost"
                },
                1,
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validReaderUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Reader,
                    SourceIP = "localhost"
                },
                1,
            },
        };

        public static readonly IEnumerable<object[]> UpdateBulkNotificationSettingsAsyncData = new List<object[]>
        {
            new object[]
            {
                new UserDto
                {
                    Id = _validSuperAdminUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.SuperAdmin,
                    SourceIP = "localhost"
                },
                new BulkNotificationSettingsDto
                {
                    UserIds = new HashSet<string> { _validAdministratorUserId, _validContributorUserId, _validReaderUserId },
                    IsEnabled = false,
                },
                _validSubscriptionIdForNotificationSetting,
                3, //number of userNotificationSettings found
                false, //notification setting enabled
                false, //is logged in user selected
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validAdministratorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                new BulkNotificationSettingsDto
                {
                    UserIds = new HashSet<string> { _validAdministratorUserId, _validContributorUserId, _validReaderUserId },
                    IsEnabled = true,
                },
                _validSubscriptionIdForNotificationSetting,
                3, //number of userNotificationSettings found
                true, //notification setting enabled
                true, //is logged in user selected
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validContributorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Contributor,
                    SourceIP = "localhost"
                },
                new BulkNotificationSettingsDto
                {
                    UserIds = new HashSet<string> { _validContributorUserId },
                    IsEnabled = false,
                },
                _validSubscriptionIdForNotificationSetting,
                1,
                false,
                true,
            },
            new object[]
            {
                new UserDto
                {
                    Id = _validContributorUserId,
                    SubscriptionId = _validSubscriptionIdForNotificationSetting,
                    UserRole = UserRole.Contributor,
                    SourceIP = "localhost"
                },
                new BulkNotificationSettingsDto
                {
                    UserIds = new HashSet<string> { _validContributorUserId },
                    IsEnabled = false,
                },
                _validSubscriptionIdForNotificationSetting,
                1,
                false,
                true,
            }
        };
    }
}