using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Handles the action of login in
        /// </summary>
        /// <param name="loginDto">The login information we use to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<UserDto>> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Logs the user in using a MFA code
        /// </summary>
        /// <param name="userId">The user we want to login</param>
        /// <param name="code">The code used to login the user</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<UserDto>> LoginMfaCodeAsync(string userId, string code);

        /// <summary>
        /// Handles the action of login in with a recovery code
        /// </summary>
        /// <param name="userId">The user id of the user that tries to login with a recovery code</param>
        /// <param name="code">The recovery code</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<UserDto>> LoginRecoveryCode(string userId, string code);

        /// <summary>
        /// Handles the action of login with an Azure Account
        /// </summary>
        /// <param name="email">The email of the user that tries to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<UserDto>> LoginAzureAccountAsync(string email);

        /// <summary>
        /// Handles the action of login in with an Azure Account on Shop App
        /// </summary>
        /// <param name="loginDto">The login information the user uses to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<SimpleUserDto>> LoginSimpleAzureAccountAsync(SimpleLoginDto loginDto);

        /// <summary>
        /// Prepares the information for a successful login
        /// </summary>
        /// <param name="user">The user that tries to login</param>
        /// <param name="mfaCodeEntered">A bool that represent if the user entered a MFA code or not</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns>A response with the user information that wants to login</returns>
        Task<Response<UserDto>> PrepareSuccessfulLoginResponseAsync(ApplicationUser user, bool mfaCodeEntered, Guid? subscriptionId = null);

        /// <summary>
        /// Generates a new refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token we want to generate</param>
        /// <returns>A response of type user dto containing the new refresh token</returns>
        Task<Response<UserDto>> GetNewTokensAsync(string refreshToken);

        /// <summary>
        /// Creates a new account with a subscription tied to it
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <returns>A farpay response indicating if the registration action was successful or not</returns>
        Task<Response<FarPayResult>> CreateAccountAndSubscriptionAsync(RegistrationDto registrationDto);

        /// <summary>
        /// Creates a new Azure account
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <returns>A string response indicating if the action was successful or not</returns>
        Task<Response<string>> CreateAzureAccountAsync(RegistrationDto registrationDto);

        /// <summary>
        /// Confirms the email of the user
        /// </summary>
        /// <param name="emailConfirmationDto">The email we want to confirm</param>
        /// <returns>A bool representing if the operation was successful or failed</returns>
        Task<bool> ConfirmEmailAsync(EmailConfirmationDto emailConfirmationDto);

        /// <summary>
        /// Checks if the email is duplicate
        /// </summary>
        /// <param name="email">The email we want to check if it has a duplicate</param>
        /// <returns>A bool determining if the email is duplicate or not</returns>
        Task<bool> CheckIfEmailIsDuplicateAsync(string email);

        /// <summary>
        /// Confirms the email that the user changed
        /// </summary>
        /// <param name="emailConfirmationDto">The email that we wanted to change</param>
        /// <returns>A bool representing if the operation was successful or failed</returns>
        Task<bool> ConfirmChangeEmailAsync(EmailConfirmationDto emailConfirmationDto);

        /// <summary>
        /// Sends an email containing the information for reseting the password
        /// </summary>
        /// <param name="email">The email we want to send the information about password reset</param>
        /// <returns>Void</returns>
        Task SendPasswordResetEmailAsync(string email);

        /// <summary>
        /// Handles the action of reseting the password
        /// </summary>
        /// <param name="passwordResetDto">The password information we want to reset</param>
        /// <returns>A bool indicating if the action was successful or not</returns>
        Task<bool> ResetPasswordAsync(PasswordResetDto passwordResetDto);

        /// <summary>
        /// Generates a key for authentication
        /// </summary>
        /// <param name="userId">The user we want to generate a key for</param>
        /// <returns>A string that indicates will be equal to the key if the operation was successful or null if it failed</returns>
        Task<string> GenerateAuthenticatorKeyAsync(string userId);

        /// <summary>
        /// Enables the two factor authentication of an EA account
        /// </summary>
        /// <param name="userId">The id of the user that the two factor authentication option will be enabled</param>
        /// <param name="token">The token of the subscription</param>
        /// <returns>A response of type user with the TwoFactorRequired property set to true</returns>
        Task<Response<UserDto>> EnableTwoFactorAsync(string userId, string token);

        /// <summary>
        /// Gets the recovery codes for a specific user
        /// </summary>
        /// <param name="userId">The user we want to get the recovery codes for</param>
        /// <returns>Returns the number of recovery codes that are still available for the user</returns>
        Task<int> GetRecoveryCodesNumberAsync(string userId);

        /// <summary>
        /// Generates a new recovery code for two factor authentication option
        /// </summary>
        /// <param name="userId">The user we want to generate the new recovery code</param>
        /// <returns>An enumerable of type string containing the recovery codes</returns>
        Task<IEnumerable<string>> GenerateNewTwoFactorRecoveryCodesAsync(string userId);

        /// <summary>
        /// Handles the action of removing the old refresh token
        /// </summary>
        /// <returns>Void</returns>
        Task RemoveOldRefreshTokensAsync();

        /// <summary>
        /// Checks if the refresh token is active
        /// </summary>
        /// <param name="userId">The user id that we want to check if the refresh token is active</param>
        /// <param name="refreshToken">The refresh token we want to check if it is active</param>
        /// <returns>A bool representing if the refresh token is active or not</returns>
        Task<bool> IsRefreshTokenActiveAsync(string userId, string refreshToken);

        /// <summary>
        /// Handles the action of revoking a token of a user
        /// </summary>
        /// <param name="userId">The user id from which we want to revoke a token</param>
        /// <param name="refreshToken">The refresh token we want to revoke</param>
        /// <returns>Void</returns>
        Task RevokeRefreshTokenAsync(string userId, string refreshToken);

        /// <summary>
        /// Handles the action of revoking all refresh tokens of a user
        /// </summary>
        /// <param name="userId">The user id from which we want to revoke all refresh tokens</param>
        /// <returns>Void</returns>
        Task RevokeAllRefreshTokensForUserAsync(string userId);
    }
}