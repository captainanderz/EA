using Microsoft.AspNetCore.Identity;
using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Gets the user based on his user id
        /// </summary>
        /// <param name="userId">The id of the user we want to get</param>
        /// <returns>The user that has the given id</returns>
        Task<UserDto> GetAsync(string userId);

        /// <summary>
        /// Gets all super admin users
        /// </summary>
        /// <returns>An enumerable with all super admin users</returns>
        Task<IEnumerable<UserDto>> GetSuperAdminUsersAsync();

        /// <summary>
        /// Disables the two factor authentication option for a user
        /// </summary>
        /// <param name="id">The id of the user we want to disable the two factor authentication option</param>
        /// <returns>Void</returns>
        Task ToggleTwoFactorAuthenticationAsync(string id);

        /// <summary>
        /// Changes the role of a user to super admin user
        /// </summary>
        /// <param name="id">The id of the user we want to make super admin</param>
        /// <returns>Void</returns>
        Task MakeUserSuperAdminAsync(string id);

        /// <summary>
        /// Changes the current subscription
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription we want to change</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<UserDto> ChangeSubscriptionAsync(Guid subscriptionId);

        /// <summary>
        /// Reloads the current subscription
        /// </summary>
        /// <returns>A response with the user information that wants to login</returns>
        Task<UserDto> ReloadSubscriptionAsync();

        /// <summary>
        /// Finds a specific user based on the subscription he is part of
        /// </summary>
        /// <returns>An enumerable with all the users in a given subscription</returns>
        Task<IEnumerable<UserDto>> FindUsersBySubscriptionAsync();

        /// <summary>
        /// Removes users from a given subscription
        /// </summary>
        /// <param name="userIds">The ids of the users we want to remove</param>
        /// <returns>Void</returns>
        Task RemoveUsersBySubscriptionAsync(IEnumerable<string> userIds);

        /// <summary>
        /// Changes the role of a user 
        /// </summary>
        /// <param name="bulkChangeUsersRoleDto">A dto containing the user roles of more than one users</param>
        /// <returns>Void</returns>
        Task ChangeUsersRoleAsync(BulkChangeUsersRoleDto bulkChangeUsersRoleDto);

        /// <summary>
        /// Checks if the user invited in the subscription already has an account
        /// </summary>
        /// <param name="registerInvitationDto">The invite registration we send to the user we want to register by invitation</param>
        /// <returns>A bool determining if the user already have an account or not</returns>
        Task<bool> IsInvitedUserAlreadyRegistered(InvitedUserDto registerInvitationDto);

        /// <summary>
        /// Invite a new user to a subscription by invitation
        /// </summary>
        /// <param name="userInvitationDto">The invitation sent to the user</param>
        /// <param name="loggedInUser">The user currently logged in</param>
        /// <returns>Void</returns>
        Task InviteUserAsync(UserInvitationDto userInvitationDto, UserDto userDto);

        /// <summary>
        /// Creates a new account by accessing the invitation to a subscription by an admin or a super admin user
        /// </summary>
        /// <param name="registerInvitationDto">The invite registration we send to the user we want to register by invitation</param>
        /// <returns>A response of type register invitation to containing information about a new account registration</returns>
        Task<Response<RegisterInvitationDto>> RegisterInvitationAsync(RegisterInvitationDto registerInvitationDto);

        /// <summary>
        /// Changes the profile picture of the current user
        /// </summary>
        /// <param name="userId">The id of the user we want to change the profile picture for</param>
        /// <param name="picture">The picture we want to change</param>
        /// <returns>Void</returns>
        Task ChangeProfilePictureAsync(string userId, MemoryStream picture);

        /// <summary>
        /// Deletes the profile picture of a specific user
        /// </summary>
        /// <param name="userId">The id of the user we want to delete the profile picture</param>
        /// <returns>Void</returns>
        Task DeleteProfilePictureAsync(string userId);

        /// <summary>
        /// Changes the user settings
        /// </summary>
        /// <param name="userId">The id of the user we want to change settings for</param>
        /// <param name="dto">The user settings dto containing the change settings information</param>
        /// <returns></returns>
        Task<bool> ChangeSettingsAsync(string userId, UserSettingsDto dto);

        /// <summary>
        /// Changes the password of an account
        /// </summary>
        /// <param name="userId">The id of the user we want to change the password for</param>
        /// <param name="dto">The information about the new password that was changed</param>
        /// <returns>An identity result containing the new changed password for the account of the given user</returns>
        Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task RemoveUserSuperAdminAsync(string id);
    }
}