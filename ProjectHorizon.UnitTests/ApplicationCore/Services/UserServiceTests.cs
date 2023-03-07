using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class UserServiceTests : IClassFixture<UserServiceFixture>
    {
        private readonly IUserService _userService;
        private readonly IApplicationDbContext _context;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;

        public UserServiceTests(UserServiceFixture userServiceFixture)
        {
            _userService = userServiceFixture.Services.GetRequiredService<IUserService>();
            _context = userServiceFixture.Services.GetRequiredService<IApplicationDbContext>();
            _loggedInUserProviderMock = userServiceFixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Theory]
        [MemberData(
            memberName: nameof(UserServiceFixture.FindUsersBySubscriptionData),
            MemberType = typeof(UserServiceFixture)
        )]
        internal async Task FindUsersBySubscriptionAsync(UserDto loggedInUser, int expectedNumberOfUsersFound)
        {
            // Arrange
            // Check MemberData

            // Act
            System.Collections.Generic.IEnumerable<UserDto>? users = await _userService.FindUsersBySubscriptionAsync();

            // Assert
            Assert.StrictEqual(expectedNumberOfUsersFound, users.Count());
        }

        [Theory]
        [MemberData(
            memberName: nameof(UserServiceFixture.ChangeUsersRoleData),
            MemberType = typeof(UserServiceFixture)
        )]
        internal async Task ChangeUsersRoleAsync(
            UserDto loggedInUser,
            BulkChangeUsersRoleDto bulkChangeUsersRoleDto,
            int expectedNrUsers)
        {
            // Arrange
            _loggedInUserProviderMock.SetLoggedInUser(loggedInUser);

            // Act
            await _userService.ChangeUsersRoleAsync(bulkChangeUsersRoleDto);

            // Assert
            int actualResult = await _context
                .SubscriptionUsers
                .CountAsync(subUser =>
                    subUser.SubscriptionId == loggedInUser.SubscriptionId &&
                    bulkChangeUsersRoleDto.UserIds.Contains(subUser.ApplicationUserId) &&
                    subUser.UserRole == bulkChangeUsersRoleDto.NewUserRole);

            Assert.StrictEqual(expectedNrUsers, actualResult);
        }

        /*
        // UserService's RemoveUsersBySubscriptionAsync method throws System.InvalidOperationException when it operates on "InMemoryDatabase"
        // Check these for more information:
        // https://stackoverflow.com/questions/50484444/system-invalidoperationexception-relational-specific-methods-can-only-be-used-w
        // https://github.com/dotnet/efcore/issues/6080

        [Theory]
        [MemberData(
            memberName: nameof(UserServiceFixture.RemoveUsersBySubscriptionData),
            MemberType = typeof(UserServiceFixture)
        )]
        internal async Task RemoveUsersBySubscriptionAsync(
            UserDto loggedInUser,
            IEnumerable<string> userIds,
            int expectedResult)
        {
            // Arrange
            // Check MemberData

            // Act
            await _userServiceFixture.UserService.RemoveUsersBySubscriptionAsync(loggedInUser, userIds);

            // Assert
            var nrNotificationsFound = await _userServiceFixture.Db
                .Notifications
                .CountAsync(notification =>
                    notification.SubscriptionId == loggedInUser.SubscriptionId &&
                    userIds.Contains(notification.ApplicationUserId)
                );
            Assert.StrictEqual(expectedResult, nrNotificationsFound);

            var nrNotificationSettingsFound = await _userServiceFixture.Db
                .NotificationSettings
                .CountAsync(notificationSetting =>
                    notificationSetting.SubscriptionId == loggedInUser.SubscriptionId &&
                    userIds.Contains(notificationSetting.ApplicationUserId)
                );
            Assert.StrictEqual(expectedResult, nrNotificationSettingsFound);

            var nrUsersFound = await _userServiceFixture.Db
                .SubscriptionUsers
                .CountAsync(
                    subUser => subUser.SubscriptionId == loggedInUser.SubscriptionId &&
                    userIds.Contains(subUser.ApplicationUserId)
                );
            Assert.StrictEqual(expectedResult, nrUsersFound);
        }*/
    }
}