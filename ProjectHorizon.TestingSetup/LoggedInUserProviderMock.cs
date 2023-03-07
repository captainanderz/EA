using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;

namespace ProjectHorizon.TestingSetup
{
    public class LoggedInUserProviderMock : ILoggedInUserProvider
    {
        private UserDto currentUser;

        public UserDto GetLoggedInUser()
        {
            return currentUser;
        }

        public void SetLoggedInUser(UserDto userDto)
        {
            currentUser = userDto;
        }
    }
}