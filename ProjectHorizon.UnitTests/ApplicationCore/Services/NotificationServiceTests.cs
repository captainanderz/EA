using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class NotificationServiceTests : IClassFixture<NotificationServiceFixture>
    {
        private readonly INotificationService _notificationService;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;

        public NotificationServiceTests(NotificationServiceFixture notificationServiceFixture)
        {
            _notificationService = notificationServiceFixture.Services.GetRequiredService<INotificationService>();
            _loggedInUserProviderMock = notificationServiceFixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Theory]
        [MemberData(
            memberName: nameof(NotificationServiceFixture.UpdateNotificationSettingAsyncData),
            MemberType = typeof(NotificationServiceFixture)
        )]
        internal async Task UpdateNotificationSettingAsync(NotificationSettingDto notificationSettingDto, UserDto loggedInUser, bool expected)
        {
            // Arrange
            _loggedInUserProviderMock.SetLoggedInUser(loggedInUser);

            // Act
            NotificationSettingDto? result = await _notificationService.UpdateNotificationSettingAsync(notificationSettingDto);

            // Assert
            Assert.StrictEqual(expected, result.IsEnabled);
        }

        [Theory]
        [MemberData(
            memberName: nameof(NotificationServiceFixture.FindUsersNotificationSettingsBySubscriptionAsyncData),
            MemberType = typeof(NotificationServiceFixture)
        )]
        internal async Task FindUsersNotificationSettingsBySubscriptionAsync(UserDto loggedInUser, int nrUsersFound)
        {
            //Arrange
            //Check MemberData

            //Act
            IEnumerable<UserNotificationSettingDto>? usersNotificationSettingsDto = await _notificationService
                .FindUsersNotificationSettingsBySubscriptionAsync(loggedInUser);

            //Assert
            Assert.StrictEqual(nrUsersFound, usersNotificationSettingsDto.Count());
        }

        [Theory]
        [MemberData(
            memberName: nameof(NotificationServiceFixture.UpdateBulkNotificationSettingsAsyncData),
            MemberType = typeof(NotificationServiceFixture)
        )]
        internal async Task UpdateBulkNotificationSettingsAsync(
            UserDto loggedInUser,
            BulkNotificationSettingsDto bulkNotificationSettingsDto,
            Guid validSubscriptionId,
            int nrUsersFound,
            bool isNotificationSettingEnabled,
            bool isLoggedInUserSelected)
        {
            //Arrange
            _loggedInUserProviderMock.SetLoggedInUser(loggedInUser);

            //Act
            await _notificationService
                .UpdateBulkNotificationSettingsAsync(validSubscriptionId, bulkNotificationSettingsDto);

            IEnumerable<UserNotificationSettingDto>? usersNotificationSettings = await _notificationService
                .FindUsersNotificationSettingsBySubscriptionAsync(loggedInUser);

            //Assert
            Assert.StrictEqual(nrUsersFound, usersNotificationSettings.Count());

            Assert.All(usersNotificationSettings,
                (userNotificationSettingDto) =>
                {
                    Assert.StrictEqual(isLoggedInUserSelected, bulkNotificationSettingsDto.UserIds.Contains(loggedInUser.Id));
                    Assert.StrictEqual(isNotificationSettingEnabled, userNotificationSettingDto.IsNewApplication);
                    Assert.StrictEqual(isNotificationSettingEnabled, userNotificationSettingDto.IsNewVersion);
                    Assert.StrictEqual(isNotificationSettingEnabled, userNotificationSettingDto.IsManualApproval);
                    Assert.StrictEqual(isNotificationSettingEnabled, userNotificationSettingDto.IsSuccessfulDeployment);
                    Assert.StrictEqual(isNotificationSettingEnabled, userNotificationSettingDto.IsFailedDeployment);
                });
        }

        [Fact]
        public async Task GetDataForUnreadNotificationsEmail_ReturnsCorrectData()
        {
            //Act
            IEnumerable<SubscriptionUser>? result = await _notificationService.GetDataForUnreadNotificationsEmail();

            //Assert
            Assert.StrictEqual(2, result.Count());
        }
    }
}