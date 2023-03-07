using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class UserInvitationTest : IClassFixture<UserInvitationTestFixture>
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly UserInvitationTestFixture _userInvitationTestFixture;
        private readonly LoggedInUserProviderMock _loggedInUserProviderMock;

        public UserInvitationTest(UserInvitationTestFixture userInvitationTestFixture)
        {
            _userInvitationTestFixture = userInvitationTestFixture;
            _userService = _userInvitationTestFixture.Services.GetRequiredService<IUserService>();
            _authService = _userInvitationTestFixture.Services.GetRequiredService<IAuthService>();
            _loggedInUserProviderMock = _userInvitationTestFixture.Services.GetRequiredService<ILoggedInUserProvider>() as LoggedInUserProviderMock;
        }

        [Theory]
        [MemberData(
            memberName: nameof(UserInvitationTestFixture.InviteUserAsyncData),
            MemberType = typeof(UserInvitationTestFixture)
        )]
        internal async void InviteUserAsync(UserInvitationDto userInvitationDto, UserDto loggedInUser)
        {
            //Arrange
            _loggedInUserProviderMock.SetLoggedInUser(loggedInUser);

            //Act & Assert
            await _userService.InviteUserAsync(userInvitationDto, loggedInUser);
        }

        [Fact]
        internal async void InviteExistingUserOnExistingSubscriptionAsync()
        {
            //Arrange
            //existing user on existing subscription
            UserInvitationDto? userInvitationDto = new UserInvitationDto
            {
                FirstName = _userInvitationTestFixture.ValidContributorUserFirstName,
                LastName = _userInvitationTestFixture.ValidContributorUserLastName,
                UserRole = UserRole.Contributor,
                Email = UserInvitationTestFixture.EmailAlreadyOnSub
            };
            UserDto? loggedInUser = new UserDto
            {
                Id = UserInvitationTestFixture.ValidAdministratorUserId,
                SubscriptionId = UserInvitationTestFixture.ValidSubscriptionIdTwo,
                UserRole = UserRole.Administrator,
                SourceIP = "localhost"
            };

            _loggedInUserProviderMock.SetLoggedInUser(loggedInUser);

            //Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.InviteUserAsync(userInvitationDto, loggedInUser));
        }

        [Theory]
        [MemberData(
            memberName: nameof(UserInvitationTestFixture.RegisterInvitationAsyncData),
            MemberType = typeof(UserInvitationTestFixture))]
        internal async void RegisterInvitationAsync(RegisterInvitationDto registerInvitationDto, bool expectedResult)
        {
            //Arrange
            //Check MemberData

            //Act
            Response<RegisterInvitationDto>? result = await _userService.RegisterInvitationAsync(registerInvitationDto);

            //Assert
            Assert.StrictEqual(expectedResult, result.IsSuccessful);
        }

        [Theory]
        [MemberData(
            memberName: nameof(UserInvitationTestFixture.CreateAzureAccountAsyncData),
            MemberType = typeof(UserInvitationTestFixture))]
        internal async void CreateAzureAccountAsync(RegistrationDto registrationDto, bool expectedResult)
        {
            //Arrange
            //Check MemberData

            //Act
            Response<string>? result = await _authService.CreateAzureAccountAsync(registrationDto);

            //Assert
            Assert.StrictEqual(expectedResult, result.IsSuccessful);
        }
    }
}