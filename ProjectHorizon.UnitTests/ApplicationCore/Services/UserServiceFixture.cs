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
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class UserServiceFixture : DbServiceFixture
    {
        private static readonly Guid _validSubscriptionId;
        private static readonly Guid _validSubscriptionIdForRemove;

        private static readonly string _validSuperAdminUserId;
        private static readonly string _validAdministratorUserId;
        private static readonly string _validContributorUserId;
        private static readonly string _validReaderUserId;

        public static readonly TheoryData<UserDto, int> FindUsersBySubscriptionData;
        public static readonly TheoryData<UserDto, BulkChangeUsersRoleDto, int> ChangeUsersRoleData;
        public static readonly TheoryData<UserDto, IEnumerable<string>, int> RemoveUsersBySubscriptionData;

        static UserServiceFixture()
        {
            _validSubscriptionId = Guid.NewGuid();
            _validSubscriptionIdForRemove = Guid.NewGuid();

            _validSuperAdminUserId = Guid.NewGuid().ToString();
            _validAdministratorUserId = Guid.NewGuid().ToString();
            _validContributorUserId = Guid.NewGuid().ToString();
            _validReaderUserId = Guid.NewGuid().ToString();

            FindUsersBySubscriptionData = new TheoryData<UserDto, int>
            {
                {
                    new UserDto
                    {
                        Id = _validSuperAdminUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.SuperAdmin,
                        SourceIP = "localhost"
                    }, // logged in user
                    3 //expected number of users found
                },
                {
                    new UserDto
                    {
                        Id = _validAdministratorUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.SuperAdmin,
                        SourceIP = "localhost"
                    },
                    3
                },
                {
                    new UserDto
                    {
                        Id = _validContributorUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.Contributor,
                        SourceIP = "localhost"
                    },
                    3
                },
                {
                    new UserDto
                    {
                        Id = _validReaderUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.Reader,
                        SourceIP = "localhost"
                    },
                    3
                },
            };

            ChangeUsersRoleData = new TheoryData<UserDto, BulkChangeUsersRoleDto, int>
            {
                {
                    new UserDto
                    {
                        Id = _validSuperAdminUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.SuperAdmin,
                        SourceIP = "localhost"
                    }, // logged in user
                    new BulkChangeUsersRoleDto
                    {
                        UserIds = new HashSet<string> { _validAdministratorUserId, _validContributorUserId, _validReaderUserId },
                        NewUserRole = UserRole.Reader
                    },
                    3 // expected number of users to change
                },
                {
                    new UserDto
                    {
                        Id = _validSuperAdminUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.SuperAdmin,
                        SourceIP = "localhost"
                    },
                    new BulkChangeUsersRoleDto
                    {
                        UserIds = new HashSet<string> { _validAdministratorUserId, _validContributorUserId, _validReaderUserId },
                        NewUserRole = UserRole.Administrator
                    },
                    3
                },
                {
                    new UserDto
                    {
                        Id = _validAdministratorUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.Administrator,
                        SourceIP = "localhost"
                    },
                    new BulkChangeUsersRoleDto
                    {
                        UserIds = new HashSet<string> { _validContributorUserId, _validReaderUserId },
                        NewUserRole = UserRole.Contributor
                    },
                    2
                },
                {
                    new UserDto
                    {
                        Id = _validAdministratorUserId,
                        SubscriptionId = _validSubscriptionId,
                        UserRole = UserRole.Administrator,
                        SourceIP = "localhost"
                    },
                    new BulkChangeUsersRoleDto
                    {
                        UserIds = new HashSet<string> { _validContributorUserId, _validReaderUserId },
                        NewUserRole = UserRole.Administrator
                    },
                    2
                }
            };

            RemoveUsersBySubscriptionData = new TheoryData<UserDto, IEnumerable<string>, int>
            {
                {
                    new UserDto
                    {
                        Id = _validAdministratorUserId,
                        SubscriptionId = _validSubscriptionIdForRemove,
                        UserRole = UserRole.Administrator,
                        SourceIP = "localhost"
                    }, // logged in user
                    new HashSet<string> { _validContributorUserId, _validReaderUserId }, //users to remove from subscription
                    0 // expected assertion result 
                },
                {
                    new UserDto
                    {
                        Id = _validSuperAdminUserId,
                        SubscriptionId = _validSubscriptionIdForRemove,
                        UserRole = UserRole.SuperAdmin,
                        SourceIP = "localhost"
                    },
                    new HashSet<string> { _validAdministratorUserId },
                    0
                },
            };
        }

        public override async Task InitializeAsync()
        {
            UserManager<ApplicationUser>? userManager = Services.GetRequiredService<UserManager<ApplicationUser>>();
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();

            Subscription? subscription = new Subscription
            {
                Id = _validSubscriptionId,
                Name = "Zwable-Subscription",
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            Subscription? subscriptionRemove = new Subscription
            {
                Id = _validSubscriptionIdForRemove,
                Name = "Zwable-Subscription-Remove",
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            context.Subscriptions.AddRange(subscription, subscriptionRemove);

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validSuperAdminUserId,
                UserName = "Test-User-SuperAdmin",
                FirstName = "Test-User-SuperAdmin first name",
                LastName = "Test-User-SuperAdmin last name",
                IsSuperAdmin = true,
            });

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validAdministratorUserId,
                UserName = "Test-User-Administrator",
                FirstName = "Test-User-Administrator first name",
                LastName = "Test-User-Administrator last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription,
                        UserRole = UserRole.Administrator
                    },
                    new()
                    {
                        Subscription = subscriptionRemove,
                        UserRole = UserRole.Administrator
                    },
                },
            });

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validContributorUserId,
                UserName = "Test-User-Contributor",
                FirstName = "Test-User-Contributor first name",
                LastName = "Test-User-Contributor last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription,
                        UserRole = UserRole.Contributor
                    },
                    new()
                    {
                        Subscription = subscriptionRemove,
                        UserRole = UserRole.Contributor
                    },
                },
            });

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = _validReaderUserId,
                UserName = "Test-User-Reader",
                FirstName = "Test-User-Reader first name",
                LastName = "Test-User-Reader last name",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription,
                        UserRole = UserRole.Reader
                    },
                    new()
                    {
                        Subscription = subscriptionRemove,
                        UserRole = UserRole.Reader
                    },
                },
            });

            context.NotificationSettings.AddRange(new NotificationSetting
            {
                Subscription = subscription,
                ApplicationUserId = _validSuperAdminUserId,
                NotificationType = NotificationType.NewApplication,
                IsEnabled = true,
            },
                new NotificationSetting
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validSuperAdminUserId,
                    NotificationType = NotificationType.NewApplication,
                    IsEnabled = false,
                },
                new NotificationSetting
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validSuperAdminUserId,
                    NotificationType = NotificationType.FailedDeployment,
                    IsEnabled = true,
                },
                new NotificationSetting
                {
                    Subscription = subscription,
                    ApplicationUserId = _validAdministratorUserId,
                    NotificationType = NotificationType.NewVersion,
                    IsEnabled = true,
                },
                new NotificationSetting
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validAdministratorUserId,
                    NotificationType = NotificationType.NewVersion,
                    IsEnabled = false,
                },
                new NotificationSetting
                {
                    Subscription = subscription,
                    ApplicationUserId = _validContributorUserId,
                    NotificationType = NotificationType.ManualApproval,
                    IsEnabled = true,
                },
                new NotificationSetting
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validContributorUserId,
                    NotificationType = NotificationType.ManualApproval,
                    IsEnabled = false,
                },
                new NotificationSetting
                {
                    Subscription = subscription,
                    ApplicationUserId = _validReaderUserId,
                    NotificationType = NotificationType.SuccessfulDeployment,
                    IsEnabled = true,
                },
                new NotificationSetting
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validReaderUserId,
                    NotificationType = NotificationType.SuccessfulDeployment,
                    IsEnabled = false,
                });

            context.Notifications.AddRange(new Notification
            {
                Subscription = subscription,
                ApplicationUserId = _validSuperAdminUserId,
                Message = "Notification SuperAdmin-User",
                Type = NotificationType.NewVersion
            },
                new Notification
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validSuperAdminUserId,
                    Message = "Notification SuperAdmin-User Remove",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscription,
                    ApplicationUserId = _validAdministratorUserId,
                    Message = "Notification Administrator-User",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validAdministratorUserId,
                    Message = "Notification Administrator-User Remove",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscription,
                    ApplicationUserId = _validContributorUserId,
                    Message = "Notification Contributor-User",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validContributorUserId,
                    Message = "Notification Contributor-User Remove",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscription,
                    ApplicationUserId = _validReaderUserId,
                    Message = "Notification Reader-User",
                    Type = NotificationType.NewVersion
                },
                new Notification
                {
                    Subscription = subscriptionRemove,
                    ApplicationUserId = _validReaderUserId,
                    Message = "Notification Reader-User Remove",
                    Type = NotificationType.NewVersion
                });

            await context.SaveChangesAsync();
        }
    }
}