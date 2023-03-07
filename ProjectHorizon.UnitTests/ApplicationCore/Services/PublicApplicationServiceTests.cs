using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class PublicApplicationServiceTests : IClassFixture<BaseServiceFixture>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPublicApplicationService _publicApplicationService;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;
        private readonly BaseServiceFixture _fixture;

        public PublicApplicationServiceTests(BaseServiceFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Services.GetRequiredService<IApplicationDbContext>();
            _publicApplicationService = fixture.Services.GetRequiredService<IPublicApplicationService>();
            _loggedInUserProviderMock = fixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Fact]
        public async Task ListPublicApplicationsPagedAsync_ReturnsCorrectData()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.PublicApplications.AddRange(
                new()
                {
                    Name = "Skype",
                    Version = "7.1",
                    Publisher = "Microsoft",
                },
                new()
                {
                    Name = "Chrome",
                    Version = "88.123",
                    Publisher = "Google",
                },
                new()
                {
                    Name = "Photoshop",
                    Version = "5.4",
                    Publisher = "Adobe",
                }
            );

            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.PublicApplicationDto>? initialPagedResult = await _publicApplicationService.ListPublicApplicationsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: null);

            await _context.SaveChangesAsync();

            // Act
            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.PublicApplicationDto>? actualPagedResult = await _publicApplicationService.ListPublicApplicationsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: null);

            System.Collections.Generic.IEnumerable<int>? actualAllPublicApplicationIds = await _publicApplicationService.ListPublicApplicationsIdsAsync();

            // Assert
            Assert.StrictEqual(3 + initialPagedResult.PageItems.Count(), actualPagedResult.PageItems.Count());
            Assert.StrictEqual(3 + initialPagedResult.AllItemsCount, actualPagedResult.AllItemsCount);
            Assert.StrictEqual(actualPagedResult.AllItemsCount, actualAllPublicApplicationIds.Count());
        }

        [Fact]
        public async Task ListPublicApplicationsPagedAsync_FilteredBySearchTerm_ReturnsCorrectData()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.Entities.Subscription? subscription = _context.Subscriptions.Add(new()
            {
                Name = "sub" + Guid.NewGuid(),
                CompanyName = "",
                Email = "",
                City = "",
                Country = "",
                ZipCode = "",
                VatNumber = "",
                State = "",
                CustomerNumber = ""
            }).Entity;

            _context.PublicApplications.AddRange(
                new()
                {
                    Name = "Notepad++",
                    Version = "7.8.8",
                    Publisher = "Notepad++",
                },
                new()
                {
                    Name = "Node.js",
                    Version = "14.16.0",
                    Publisher = "Node.js Foundation",
                },
                new()
                {
                    Name = "PuTTY",
                    Version = "0.74.0.0",
                    Publisher = "Simon Tatham",
                },
                new()
                {
                    Name = "Python Launcher",
                    Version = "3.9.7280.0",
                    Publisher = "Python Software Foundation"
                }
            );

            await _context.SaveChangesAsync();

            // Act
            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.PublicApplicationDto>? actualPagedResultFiltered = await _publicApplicationService.ListPublicApplicationsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: "no");

            // Assert
            Assert.StrictEqual(2, actualPagedResultFiltered.PageItems.Count());
            Assert.StrictEqual(2, actualPagedResultFiltered.AllItemsCount);
        }

        [Fact]
        public async Task DownloadPublicApplicationAsync_EmptyPackageName_ThrowsException()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.Entities.PublicApplication? publicApplication = _context.PublicApplications.Add(new()
            {
                Name = "Microsoft Teams",
                Version = "14.1"
            }).Entity;

            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _publicApplicationService.GetDownloadUriForPublicApplicationAsync(publicApplication.Id)
            );
        }

        [Fact]
        public async Task UpdateSubscriptionPublicApplicationAutoUpdateAsync_PerformsUpdate()
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

            ProjectHorizon.ApplicationCore.Entities.PublicApplication? publicApplication = _context.PublicApplications.Add(new()
            {
                Name = "Microsoft Teams Update",
                Version = "14.1"
            }).Entity;

            _context.SubscriptionPublicApplications.Add(new()
            {
                Subscription = subscription,
                PublicApplication = publicApplication,
                AutoUpdate = true,
                DeployedVersion = "14.0"
            });

            await _context.SaveChangesAsync();

            // Act
            bool actualAutoUpdate = await _publicApplicationService.UpdateSubscriptionPublicApplicationAutoUpdateAsync(
                publicApplication.Id,
                autoUpdate: false);

            ProjectHorizon.ApplicationCore.Entities.SubscriptionPublicApplication? actualSubPubApp = await _context
                .SubscriptionPublicApplications
                .SingleAsync(spa => spa.SubscriptionId == subscription.Id &&
                        spa.PublicApplicationId == publicApplication.Id
                );

            // Assert
            Assert.False(actualSubPubApp.AutoUpdate);
            Assert.False(actualAutoUpdate);
        }

        [Fact]
        public async Task UpdateSubscriptionPublicApplicationManualApproveAsync_PerformsUpdate()
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

            ProjectHorizon.ApplicationCore.Entities.PublicApplication? publicApplication = _context.PublicApplications.Add(new()
            {
                Name = "Test App",
                Version = "1.0"
            }).Entity;

            _context.SubscriptionPublicApplications.Add(new()
            {
                ManualApprove = false,
                DeployedVersion = "0.1",
                Subscription = subscription,
                PublicApplication = publicApplication
            });

            await _context.SaveChangesAsync();

            // Act
            bool actualManualApprove = await _publicApplicationService.UpdateSubscriptionPublicApplicationManualApproveAsync(
                publicApplication.Id,
                manualApprove: true);

            ProjectHorizon.ApplicationCore.Entities.SubscriptionPublicApplication? subPubApp = await _context
                .SubscriptionPublicApplications
                .SingleAsync(spa =>
                    spa.SubscriptionId == subscription.Id &&
                    spa.PublicApplicationId == publicApplication.Id
                );

            // Assert
            Assert.True(subPubApp.ManualApprove);
            Assert.True(actualManualApprove);
        }

        [Fact]
        public async Task RemovePublicApplicationAsync_PerformsRemove()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.DTOs.UserDto? initialUser = _loggedInUserProviderMock.GetLoggedInUser();

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

            ProjectHorizon.ApplicationCore.Entities.PublicApplication? publicApplication = _context.PublicApplications.Add(new()
            {
                Name = "Firefox",
                Version = "7.6"
            }).Entity;

            _context.SubscriptionUsers.Add(new()
            {
                ApplicationUserId = _fixture.ValidSuperAdminUserId,
                UserRole = UserRole.SuperAdmin,
                Subscription = subscription
            });

            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidSuperAdminUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            });

            // Act
            await _publicApplicationService.RemovePublicApplicationsAsync(new int[publicApplication.Id]);

            // Assert
            ProjectHorizon.ApplicationCore.Entities.PublicApplication? actual = await _context.PublicApplications.SingleOrDefaultAsync(pa => pa.Id == publicApplication.Id);
            Assert.Null(actual);

            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }
    }
}