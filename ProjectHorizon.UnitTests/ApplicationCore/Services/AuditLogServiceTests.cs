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
    public class AuditLogServiceTests : IClassFixture<BaseServiceFixture>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;
        private readonly BaseServiceFixture _fixture;

        public AuditLogServiceTests(BaseServiceFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Services.GetRequiredService<IApplicationDbContext>();
            _auditLogService = fixture.Services.GetRequiredService<IAuditLogService>();
            _loggedInUserProviderMock = fixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Fact]
        public async Task ListAuditLogsPagedAsync_ReturnsCorrectData()
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

            _context.AuditLogs.AddRange(
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidSuperAdminUserId,
                    ActionText = "action on PublicRepository",
                    Category = AuditLogCategory.PublicRepository,
                    SourceIP = "localhost"
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidSuperAdminUserId,
                    ActionText = "action on Integrations",
                    Category = AuditLogCategory.Integrations,
                    SourceIP = "localhost"
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidAdministratorUserId,
                    ActionText = "action on UserManagement",
                    Category = AuditLogCategory.UserManagement,
                    SourceIP = "localhost"
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidContributorUserId,
                    ActionText = "action on NotificationSettings",
                    Category = AuditLogCategory.NotificationSettings,
                    SourceIP = "localhost"
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidReaderUserId,
                    ActionText = "action on Approvals",
                    Category = AuditLogCategory.Approvals,
                    SourceIP = "localhost"
                },
                new()
                {
                    Subscription = subscription,
                    ApplicationUserId = _fixture.ValidReaderUserId,
                    ActionText = "action on PrivateRepository",
                    Category = AuditLogCategory.PrivateRepository,
                    SourceIP = "localhost"
                }
            );

            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidSuperAdminUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.SuperAdmin,
                SourceIP = "localhost"
            });

            // Act
            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.AuditLogDto>? actualPagedResult = await _auditLogService.ListAuditLogsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                searchTerm: null,
                null,
                null,
                AuditLogCategory.AllCategories);

            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.AuditLogDto>? actualFilteredPagedResult = await _auditLogService.ListAuditLogsPagedAsync(
               pageNumber: 1,
               pageSize: 20,
               searchTerm: "repo",
               null,
               null,
               AuditLogCategory.AllCategories);

            // Assert
            Assert.StrictEqual(6, actualPagedResult.PageItems.Count());
            Assert.StrictEqual(6, actualPagedResult.AllItemsCount);

            Assert.StrictEqual(2, actualFilteredPagedResult.PageItems.Count());
            Assert.StrictEqual(2, actualFilteredPagedResult.AllItemsCount);


            // they should be ordered by CreatedOn descending
            Assert.Equal(AuditLogCategory.PrivateRepository, actualPagedResult.PageItems.First().Category);
            Assert.Equal(AuditLogCategory.PrivateRepository, actualFilteredPagedResult.PageItems.First().Category);
        }

        [Fact]
        public async Task CreateValidAuditLog_WithAdministrator_ListReturnsValidData()
        {
            // Arrange
            ProjectHorizon.ApplicationCore.DTOs.UserDto? initialUser = _loggedInUserProviderMock.GetLoggedInUser();
            string testActionText = "Test SingleSignOn audit log message";

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

            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidAdministratorUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            });

            // Act
            await _auditLogService.GenerateAuditLogAsync(AuditLogCategory.SingleSignOn, testActionText);
            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.AuditLogDto>? auditLogs = await _auditLogService.ListAuditLogsPagedAsync(
                1,
                20,
                null,
                null,
                null,
                AuditLogCategory.SingleSignOn);

            // Assert
            bool isAuditLogGenerated = false;
            foreach (ProjectHorizon.ApplicationCore.DTOs.AuditLogDto? auditLog in auditLogs.PageItems)
            {
                if (auditLog.ActionText == testActionText)
                {
                    isAuditLogGenerated = true;
                    break;
                }
            }

            Assert.True(isAuditLogGenerated);
            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }
    }
}