using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using ProjectHorizon.UnitTests.ApplicationCore.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.Infrastructure
{
    public class GraphConfigServiceTest : IClassFixture<BaseServiceFixture>
    {
        private readonly IApplicationDbContext _context;
        private readonly IGraphConfigService _graphConfigService;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;
        private readonly BaseServiceFixture _fixture;

        public GraphConfigServiceTest(BaseServiceFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Services.GetRequiredService<IApplicationDbContext>();
            _graphConfigService = fixture.Services.GetRequiredService<IGraphConfigService>();
            _loggedInUserProviderMock = fixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Fact]
        public async Task CreateGraphConfigAsync_WithValidGraphConfig()
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
                ApplicationUserId = _fixture.ValidAdministratorUserId,
                UserRole = UserRole.Administrator
            });


            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidAdministratorUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            });

            GraphConfigDto? graphConfigDto = new GraphConfigDto
            {
                SubscriptionId = subscription.Id,
                ClientId = "testClientId",
                ClientSecret = "testClientSecret",
                Tenant = "testTenantId"
            };

            // Act
            await _graphConfigService.CreateGraphConfigAsync(graphConfigDto, subscription.Id);

            // Assert
            bool result = await _graphConfigService.HasGraphConfigAsync(subscription.Id);
            Assert.True(result, "GraphConfig should be set");

            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }

        [Fact]
        public async Task RemoveGraphConfigAsync_RemoveSuccessful()
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
                ApplicationUserId = _fixture.ValidAdministratorUserId,
                UserRole = UserRole.Administrator
            });


            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidAdministratorUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            });

            GraphConfigDto? graphConfigDto = new GraphConfigDto
            {
                SubscriptionId = subscription.Id,
                ClientId = "testClientId",
                ClientSecret = "testClientSecret",
                Tenant = "testTenantId"
            };

            await _graphConfigService.CreateGraphConfigAsync(graphConfigDto, subscription.Id);

            // Act
            await _graphConfigService.RemoveGraphConfigAsync(subscription.Id);

            // Assert
            bool result = await _graphConfigService.HasGraphConfigAsync(subscription.Id);
            Assert.False(result, "GraphConfig should not exist on subscription");
            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }

        [Fact]
        public async Task GetGraphConfig_ValidDtoRetrieved()
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
                ApplicationUserId = _fixture.ValidAdministratorUserId,
                UserRole = UserRole.Administrator
            });

            await _context.SaveChangesAsync();

            _loggedInUserProviderMock.SetLoggedInUser(new()
            {
                Id = _fixture.ValidAdministratorUserId,
                SubscriptionId = subscription.Id,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            });

            GraphConfigDto? inputGraphConfigDto = new GraphConfigDto
            {
                SubscriptionId = subscription.Id,
                ClientId = "testClientId",
                ClientSecret = "testClientSecret",
                Tenant = "testTenantId",
            };

            await _graphConfigService.CreateGraphConfigAsync(inputGraphConfigDto, subscription.Id);

            // Act
            GraphConfigDto? graphConfigDtoResult = await _graphConfigService.GetGraphConfigAsync(subscription.Id);

            // Assert
            Assert.Equal(inputGraphConfigDto.ClientId, graphConfigDtoResult.ClientId);
            Assert.Equal(inputGraphConfigDto.ClientSecret, graphConfigDtoResult.ClientSecret);
            Assert.Equal(inputGraphConfigDto.Tenant, graphConfigDtoResult.Tenant);

            _loggedInUserProviderMock.SetLoggedInUser(initialUser);
        }
    }
}