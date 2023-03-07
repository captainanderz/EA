using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Utility;
using ProjectHorizon.TestingSetup;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class PrivateApplicationsServiceTest : IClassFixture<BaseServiceFixture>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPrivateApplicationService _privateApplicationService;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;
        private readonly BaseServiceFixture _fixture;

        public PrivateApplicationsServiceTest(BaseServiceFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Services.GetRequiredService<IApplicationDbContext>();
            _privateApplicationService = fixture.Services.GetRequiredService<IPrivateApplicationService>();
            _loggedInUserProviderMock = fixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
            _mapper = fixture.Services.GetRequiredService<IMapper>();
        }

        [Fact]
        public async Task ListPrivateApplicationsPagedAsync_ReturnsCorrectData()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "Sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.PrivateApplications.AddRange(
                new()
                {
                    Name = "Notepad++",
                    Version = "7.8.8",
                    Publisher = "Notepad++",
                    Subscription = subscription,
                },
                new()
                {
                    Name = "Node.js",
                    Version = "14.16.0",
                    Publisher = "Node.js Foundation",
                    Subscription = subscription,
                },
                new()
                {
                    Name = "Skype",
                    Version = "7.1",
                    Publisher = "Microsoft",
                    Subscription = subscription
                },
                new()
                {
                    Name = "Chrome",
                    Version = "88.123",
                    Publisher = "Google",
                    Subscription = subscription
                },
                new()
                {
                    Name = "Photoshop",
                    Version = "5.4",
                    Publisher = "Adobe",
                    Subscription = subscription,
                }
            );

            await _context.SaveChangesAsync();

            // Act
            PagedResult<PrivateApplicationDto>? actualPagedResult = await _privateApplicationService.ListPrivateApplicationsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: null);

            System.Collections.Generic.IEnumerable<int>? actualPrivateAppIds = await _privateApplicationService
                .ListPrivateApplicationsIdsAsync();

            int expectedNumberOfPrivateApplications = await _context
                .PrivateApplications
                .CountAsync(pa => pa.SubscriptionId == subscription.Id);

            // Assert
            Assert.StrictEqual(5, actualPagedResult.PageItems.Count());
            Assert.StrictEqual(5, actualPagedResult.AllItemsCount);
            Assert.StrictEqual(expectedNumberOfPrivateApplications, actualPrivateAppIds.Count());
        }

        [Fact]
        public async Task ListPrivateApplicationsPagedAsync_FilteredBySearchTerm_ReturnsCorrectData()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "Sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.PrivateApplications.AddRange(
                new()
                {
                    Name = "Notepad++",
                    Version = "7.8.8",
                    Publisher = "Notepad++",
                    Subscription = subscription,
                },
                new()
                {
                    Name = "Node.js",
                    Version = "14.16.0",
                    Publisher = "Node.js Foundation",
                    Subscription = subscription,
                },
                new()
                {
                    Name = "Skype",
                    Version = "7.1",
                    Publisher = "Microsoft",
                    Subscription = subscription
                },
                new()
                {
                    Name = "Chrome",
                    Version = "88.123",
                    Publisher = "Google",
                    Subscription = subscription
                },
                new()
                {
                    Name = "Photoshop",
                    Version = "5.4",
                    Publisher = "Adobe",
                    Subscription = subscription,
                    ModifiedOn = DateTimeOffset.UtcNow.DateTime.AddMonths(1)
                }
           );

            await _context.SaveChangesAsync();

            // Act
            PagedResult<PrivateApplicationDto>? actualFilteredPagedResult = await _privateApplicationService.ListPrivateApplicationsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: "no");

            // Assert
            Assert.StrictEqual(2, actualFilteredPagedResult.PageItems.Count());
            Assert.StrictEqual(2, actualFilteredPagedResult.AllItemsCount);
        }

        [Fact]
        public async Task RemovePrivateApplicationAsync_PerformsRemove()
        {
            // Arrange
            UserDto? initialUser = _loggedInUserProviderMock.GetLoggedInUser();

            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "Sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.SubscriptionUsers.Add(new()
            {
                Subscription = subscription,
                ApplicationUserId = _fixture.ValidSuperAdminUserId,
                UserRole = UserRole.SuperAdmin
            });

            ProjectHorizon.ApplicationCore.Entities.PrivateApplication? privateApplication = _context.PrivateApplications.Add(new()
            {
                Name = "Firefox",
                Version = "7.6",
                Subscription = subscription
            }).Entity;

            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidSuperAdminUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.SuperAdmin,
                SourceIP = "localhost"
            });

            // Act
            await _privateApplicationService.RemovePrivateApplicationsAsync(new int[privateApplication.Id]);

            // Assert
            ProjectHorizon.ApplicationCore.Entities.PrivateApplication? result = _context.PrivateApplications.SingleOrDefault(pa => pa.Id == privateApplication.Id);
            Assert.Null(result);

            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }


        [Fact]
        public async Task AddOrUpdatePrivateApplicationAsync_PerformsAdd()
        {
            // Arrange
            UserDto? initialUser = _loggedInUserProviderMock.GetLoggedInUser();

            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "Sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.SubscriptionUsers.AddRange(
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidAdministratorUserId,
                    UserRole = UserRole.Administrator
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidContributorUserId,
                    UserRole = UserRole.Contributor
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidReaderUserId,
                    UserRole = UserRole.Reader
                }
            );

            _context.NotificationSettings.Add(new()
            {
                Subscription = subscription,
                ApplicationUserId = _fixture.ValidAdministratorUserId,
                NotificationType = NotificationType.NewApplication,
                IsEnabled = false,
            });

            await _context.SaveChangesAsync();

            PrivateApplicationDto? privateApplicationDto = new PrivateApplicationDto
            {
                Name = "PuTTY",
                Version = "0.74.0.0",
                Publisher = "Simon Tatham",
                RunAs32Bit = false,
                InformationUrl = @"https://www.endpointadmin.com",
            };

            string? applicationPackageName = ApplicationHelper.GetTempArchiveFileNameFromInfo(privateApplicationDto, subscription.Id);
            File.Create(Path.Combine(ApplicationHelper.GetMountPath(), applicationPackageName)).Close();

            UserDto? loggedInUserDto = new UserDto
            {
                Id = _fixture.ValidSuperAdminUserId,
                UserRole = UserRole.SuperAdmin,
                SubscriptionId = subscription.Id,
                SourceIP = "localhost"
            };

            _loggedInUserProviderMock.SetLoggedInUser(loggedInUserDto);

            // Act
            int expectedApplicationId = await _privateApplicationService
                 .AddOrUpdatePrivateApplicationAsync(privateApplicationDto);

            ProjectHorizon.ApplicationCore.Entities.PrivateApplication? actualPrivateApplication = await _context
                .PrivateApplications
                .SingleAsync(pa => pa.Id == expectedApplicationId);

            int actualNumberOfNotificationsGenerated = await _context
                .Notifications
                .CountAsync(notification =>
                    notification.SubscriptionId == loggedInUserDto.SubscriptionId &&
                    notification.Type == NotificationType.NewApplication &&
                    notification.ForPrivateRepository == true);

            // Assert
            Assert.StrictEqual(expectedApplicationId, actualPrivateApplication.Id);
            Assert.Equal(privateApplicationDto.Version, actualPrivateApplication.Version);
            Assert.Equal(privateApplicationDto.Name, actualPrivateApplication.Name);
            Assert.StrictEqual(3, actualNumberOfNotificationsGenerated);

            //Tear down
            File.Delete(Path.Combine(ApplicationHelper.GetMountPath(), applicationPackageName));
            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }

        [Fact]
        public async Task AddOrUpdatePrivateApplicationAsync_PerformsUpdate()
        {
            // Arrange
            UserDto? initialUser = _loggedInUserProviderMock.GetLoggedInUser();

            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "Sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.SubscriptionUsers.AddRange(
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidAdministratorUserId,
                    UserRole = UserRole.Administrator
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidContributorUserId,
                    UserRole = UserRole.Contributor
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidReaderUserId,
                    UserRole = UserRole.Reader
                }
            );

            _context.NotificationSettings.Add(new()
            {
                Subscription = subscription,
                ApplicationUserId = _fixture.ValidAdministratorUserId,
                NotificationType = NotificationType.NewVersion,
                IsEnabled = false,
            });

            ProjectHorizon.ApplicationCore.Entities.PrivateApplication? privateApplication = _context.PrivateApplications.Add(new()
            {
                Name = "Python Launcher",
                Version = "3.9.7280.0",
                Publisher = "Python Software Foundation",
                RunAs32Bit = false,
                InformationUrl = @"https://www.endpointadmin.com",
                Subscription = subscription
            }).Entity;

            await _context.SaveChangesAsync();


            PrivateApplicationDto? privateApplicationDto = _mapper.Map<PrivateApplicationDto>(privateApplication);
            privateApplicationDto.Version = "3.10.0.0";
            string? applicationPackageName = ApplicationHelper.GetTempArchiveFileNameFromInfo(privateApplicationDto, subscription.Id);

            File.Create(Path.Combine(ApplicationHelper.GetMountPath(), applicationPackageName)).Close();

            UserDto? loggedInUserDto = new UserDto
            {
                Id = _fixture.ValidAdministratorUserId,
                UserRole = UserRole.Administrator,
                SubscriptionId = subscription.Id,
                SourceIP = "localhost"
            };

            _loggedInUserProviderMock.SetLoggedInUser(loggedInUserDto);

            // Act
            int expectedApplicationId = await _privateApplicationService
                 .AddOrUpdatePrivateApplicationAsync(privateApplicationDto);

            ProjectHorizon.ApplicationCore.Entities.PrivateApplication? actualPrivateApplication = await _context
                .PrivateApplications
                .SingleAsync(pa => pa.Id == expectedApplicationId);

            int actualNumberOfNotificationsGenerated = await _context
                .Notifications
                .CountAsync(notification =>
                    notification.SubscriptionId == loggedInUserDto.SubscriptionId &&
                    notification.Type == NotificationType.NewVersion &&
                    notification.ForPrivateRepository == true);

            // Assert
            Assert.StrictEqual(expectedApplicationId, actualPrivateApplication.Id);
            Assert.Equal(privateApplication.Version, actualPrivateApplication.Version);
            Assert.Equal(privateApplication.Name, actualPrivateApplication.Name);
            Assert.StrictEqual(2, actualNumberOfNotificationsGenerated);

            //Tear-down
            File.Delete(Path.Combine(ApplicationHelper.GetMountPath(), applicationPackageName));
            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }
    }
}