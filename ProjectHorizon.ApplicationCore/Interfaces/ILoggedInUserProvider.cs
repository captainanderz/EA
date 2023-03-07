using ProjectHorizon.ApplicationCore.DTOs;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface ILoggedInUserProvider
    {
        /// <summary>
        /// Gets the currently logged in user
        /// </summary>
        /// <returns>A dto representing the user that is currently logged in the application</returns>
        UserDto? GetLoggedInUser();
    }
}