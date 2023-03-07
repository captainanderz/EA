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
    public class UserInvitationTestFixture : DbServiceFixture
    {
        private static readonly Guid ValidSubscriptionId = Guid.NewGuid();
        private static readonly string ValidSubscriptionName = "Zwable-Subscription-Invitation";
        internal static readonly Guid ValidSubscriptionIdTwo = Guid.NewGuid();
        private static readonly string ValidSubscriptionNameTwo = "Zwable-Subscription-Invitation-2";

        internal static readonly string ValidAdministratorUserId = Guid.NewGuid().ToString();

        internal readonly string ValidContributorUserFirstName = "InvitationTestContributorFirstName";
        internal readonly string ValidContributorUserLastName = "InvitationTestContributorLastName";
        private static readonly string ValidContributorUserId = Guid.NewGuid().ToString();

        //invitation
        private static readonly string InvitedUserFirstName = "InvitedUserFirstName";
        private static readonly string InvitedUserLastName = "InvitedUserLastName";
        private static readonly string InvitedUserEmail = "test@email.com";

        //registration from invitation
        private static readonly string EmailForRegistration = "register@email.com";
        private static readonly string AzureEmailForRegistration = "fortech@eadstest.onmicrosoft.com";
        internal static readonly string EmailAlreadyOnSub = "onsub@email.com";

        private static readonly string baseUrl = "baseUrl";

        public override async Task InitializeAsync()
        {
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();
            UserManager<ApplicationUser>? userManager = Services.GetRequiredService<UserManager<ApplicationUser>>();

            Subscription? subscription1 = new Subscription
            {
                Id = ValidSubscriptionId,
                Name = ValidSubscriptionName,
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            Subscription? subscription2 = new Subscription
            {
                Id = ValidSubscriptionIdTwo,
                Name = ValidSubscriptionNameTwo,
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            };
            context.Subscriptions.AddRange(subscription1, subscription2);

            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "InvitationSuperAdmin",
                FirstName = "InvitationSuperAdminFirstName",
                LastName = "InvitationSuperAdminLastName",
                IsSuperAdmin = true,
            });

            ApplicationUser? administratorUser = new ApplicationUser
            {
                Id = ValidAdministratorUserId,
                UserName = "InvitationAdministrator",
                FirstName = "InvitationAdministratorFirstName",
                LastName = "InvitationAdministratorLastName",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription1,
                        UserRole = UserRole.Administrator
                    },
                    new()
                    {
                        Subscription = subscription2,
                        UserRole = UserRole.Administrator
                    }
                }
            };
            await userManager.CreateAsync(administratorUser);

            await userManager.CreateAsync(new ApplicationUser
            {
                Id = ValidContributorUserId,
                UserName = "InvitationTestContributor",
                FirstName = ValidContributorUserFirstName,
                LastName = ValidContributorUserLastName,
                IsSuperAdmin = false,
                Email = EmailAlreadyOnSub,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription2,
                        ApplicationUserId = ValidContributorUserId,
                        UserRole = UserRole.Contributor
                    }
                }
            });

            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "InvitationReader",
                FirstName = "InvitationReaderFirstName",
                LastName = "InvitationReaderLastName",
                IsSuperAdmin = false,
                SubscriptionUsers = new List<SubscriptionUser>
                {
                    new()
                    {
                        Subscription = subscription1,
                        UserRole = UserRole.Reader
                    }
                }
            });

            await context.UserInvitations.AddRangeAsync(
                // new invited user
                new UserInvitation
                {
                    FirstName = "UserToRegisterFirstName",
                    LastName = "UserToRegisterLastName",
                    Email = EmailForRegistration,
                    UserRole = UserRole.Contributor,
                    InvitationToken = "invitationToken",
                    Subscription = subscription1,
                    ApplicationUser = administratorUser //user who initiated the invitation
                },

                // new invited user - Azure AD
                new UserInvitation
                {
                    FirstName = "AzureUserToRegisterFirstName",
                    LastName = "AzureUserToRegisterLastName",
                    Email = AzureEmailForRegistration,
                    UserRole = UserRole.Administrator,
                    InvitationToken = "azureInvitationToken",
                    Subscription = subscription2,
                    ApplicationUser = administratorUser
                },

                //existing contributor
                new UserInvitation
                {
                    FirstName = ValidContributorUserFirstName,
                    LastName = ValidContributorUserLastName,
                    Email = EmailAlreadyOnSub,
                    UserRole = UserRole.Contributor,
                    InvitationToken = "invitationToken",
                    ApplicationUser = administratorUser,
                    Subscription = subscription2
                });

            await context.SaveChangesAsync();
        }

        public static readonly IEnumerable<object[]> InviteUserAsyncData = new List<object[]>
        {
            //existing user to add to new subscription       
            new object[]
            {
                new UserInvitationDto
                {
                    FirstName = "InvitationContributorFirstName",
                    LastName = "InvitationContributorLastName",
                    UserRole = UserRole.Contributor,
                    Email = "contributor@email.com"
                },
                new UserDto
                {
                    Id = ValidAdministratorUserId,
                    SubscriptionId = ValidSubscriptionIdTwo,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                baseUrl,
            },

            // new user to invite
            new object[]
            {
                new UserInvitationDto
                {
                    FirstName = InvitedUserFirstName,
                    LastName = InvitedUserLastName,
                    UserRole = UserRole.Reader,
                    Email = InvitedUserEmail
                },
                new UserDto
                {
                    Id = ValidAdministratorUserId,
                    SubscriptionId = ValidSubscriptionId,
                    UserRole = UserRole.Administrator,
                    SourceIP = "localhost"
                },
                baseUrl,
            }
        };

        public static readonly IEnumerable<object[]> RegisterInvitationAsyncData = new List<object[]>
        {
            // new user registering from invite
            new object[]
            {
                new RegisterInvitationDto
                {
                    Password = "Fortech1!",
                    Email = EmailForRegistration,
                    EmailToken = "invitationToken",
                    SubscriptionName = ValidSubscriptionName
                },
                true //expected result
            },

            // invalid email token
            new object[]
            {
                new RegisterInvitationDto
                {
                    Password = "Fortech1!",
                    Email = EmailForRegistration,
                    EmailToken = "fakeToken",
                    SubscriptionName = ValidSubscriptionName
                },
                false //expected result
            },

            //existing user on invitation subscription
            new object[]
            {
                new RegisterInvitationDto
                {
                    Password = "Fortech1!",
                    Email = EmailAlreadyOnSub,
                    EmailToken = "invitationToken",
                    SubscriptionName = ValidSubscriptionNameTwo
                },
                false //expected result
            },

            //no user invitation
            new object[]
            {
                new RegisterInvitationDto
                {
                    Password = "Fortech1!",
                    Email = "fake@fake.com",
                    EmailToken = "fakeToken",
                    SubscriptionName = "invalidSub"
                },
                false //expected result
            }
        };

        public static readonly IEnumerable<object[]> CreateAzureAccountAsyncData = new List<object[]>
        {
            // new user registering from invite
            new object[]
            {
                new RegistrationDto
                {
                    Email = AzureEmailForRegistration,
                    SubscriptionEmail = AzureEmailForRegistration,
                    FirstName = "AzureFirstName",
                    LastName = "AzureLastName",
                    CompanyName = "EndpointAdminCompany",
                    City = "Copenhagen",
                    Country = "Denmark",
                    ZipCode = "123",
                    VatNumber = "456"
                },
                true //expected result
            },
            
            // new user registering from invite
            new object[]
            {
                new RegistrationDto
                {
                    Email = EmailAlreadyOnSub,
                    SubscriptionEmail = AzureEmailForRegistration,
                    FirstName = "AzureFirstName",
                    LastName = "AzureLastName",
                    CompanyName = "EndpointAdminCompany",
                    City = "Copenhagen",
                    Country = "Denmark",
                    ZipCode = "123",
                    VatNumber = "456"
                },
                false //expected result
            },
        };
    }
}