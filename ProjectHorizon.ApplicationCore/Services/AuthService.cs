using AutoMapper;
using AutoMapper.QueryableExtensions;
using log4net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Properties;
using ProjectHorizon.ApplicationCore.Utility;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class AuthService : IAuthService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IEconomicService _economicService;
        private readonly IEmailService _emailService;
        private readonly IFarPayService _farPayService;
        private readonly JwtAuthentication _jwtAuthentication;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationInformation _applicationInformation;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly Options.Environment _envOptions;

        public AuthService(
            IMapper mapper,
            IApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager,
            IOptions<JwtAuthentication> jwtAuthentication,
            IEmailService emailService,
            IFarPayService farPayService,
            IEconomicService economicService,
            IOptions<ApplicationInformation> applicationInformation,
            ILoggedInUserProvider loggedInUserProvider,
            IOptions<Options.Environment> envOptions)
        {
            _mapper = mapper;
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _emailService = emailService;
            _jwtAuthentication = jwtAuthentication.Value;
            _farPayService = farPayService;
            _economicService = economicService;
            _applicationInformation = applicationInformation.Value;
            _loggedInUserProvider = loggedInUserProvider;
            _envOptions = envOptions.Value;
        }

        /// <summary>
        /// Checks if the email is duplicate
        /// </summary>
        /// <param name="email">The email we want to check if it has a duplicate</param>
        /// <returns>A bool determining if the email is duplicate or not</returns>
        public async Task<bool> CheckIfEmailIsDuplicateAsync(string email)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(email);

            return user != null;
        }

        /// <summary>
        /// Confirms the email that the user changed
        /// </summary>
        /// <param name="emailConfirmationDto">The email that we wanted to change</param>
        /// <returns>A bool representing if the operation was successful or failed</returns>
        public async Task<bool> ConfirmChangeEmailAsync(EmailConfirmationDto emailConfirmationDto)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(emailConfirmationDto.Email);
            if (user == null)
            {
                return false;
            }

            if (user.NewUnconfirmedEmail == user.Email)
            {
                return true;
            }

            IdentityResult? result = await _userManager.ChangeEmailAsync(user, user.NewUnconfirmedEmail, emailConfirmationDto.Token);

            return result.Succeeded;
        }

        /// <summary>
        /// Confirms the email of the user
        /// </summary>
        /// <param name="emailConfirmationDto">The email we want to confirm</param>
        /// <returns>A bool representing if the operation was successful or failed</returns>
        public async Task<bool> ConfirmEmailAsync(EmailConfirmationDto emailConfirmationDto)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(emailConfirmationDto.Email);
            if (user == null)
            {
                return false;
            }

            if (user.EmailConfirmed)
            {
                return true;
            }

            IdentityResult? result = await _userManager.ConfirmEmailAsync(user, emailConfirmationDto.Token);

            return result.Succeeded;
        }

        /// <summary>
        /// Creates a new account with a subscription tied to it
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <returns>A farpay response indicating if the registration action was successful or not</returns>
        public async Task<Response<FarPayResult>> CreateAccountAndSubscriptionAsync(RegistrationDto registrationDto)
        {
            Response<FarPayResult> result = new Response<FarPayResult>
            {
                ErrorMessage = $"We weren't able to create your account. " +
                $"Please try again. " +
                $"Contact support if the problem persists.",
                IsSuccessful = false
            };

            ApplicationUser? applicationUser = await CreateAccountAsync(registrationDto);

            if (applicationUser != null)
            {
                Subscription subscription;
                try
                {
                    subscription = await CreateSubscriptionAsync(registrationDto, applicationUser);
                }
                catch
                {
                    return result;
                }

                result = await CreatePaymentInformation(registrationDto, subscription);

                await SendConfirmEmailAsync(applicationUser);
            }
            else
            {
                result.ErrorMessage = $"An account with {registrationDto.Email} is already registered.";
                return result;
            }

            return result;
        }

        /// <summary>
        /// Creates a new Azure account
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <returns>A string response indicating if the action was successful or not</returns>
        public async Task<Response<string>> CreateAzureAccountAsync(RegistrationDto registrationDto)
        {
            Response<string>? response = new Response<string>
            {
                IsSuccessful = false,
                ErrorMessage = "Registration with Azure AD failed! Invalid or expired invitation."
            };

            UserInvitation userInvitation = await _applicationDbContext.UserInvitations
                .SingleOrDefaultAsync(x => x.Email == registrationDto.Email);

            if (userInvitation == null)
            {
                Response<FarPayResult>? createSubResponse = await CreateAccountAndSubscriptionAsync(registrationDto);
                response.ErrorMessage = createSubResponse.IsSuccessful ? null : "Registration with Azure AD failed! Could not create account.";
                response.IsSuccessful = createSubResponse.IsSuccessful;

                return response;
            }
            else
            {
                return await CreateAzureAccountFromInvitationAsync(registrationDto, userInvitation);
            }
        }

        /// <summary>
        /// Enables the two factor authentication of an EA account
        /// </summary>
        /// <param name="userId">The id of the user that the two factor authentication option will be enabled</param>
        /// <param name="token">The token of the subscription</param>
        /// <returns>A response of type user with the TwoFactorRequired property set to true</returns>
        public async Task<Response<UserDto>> EnableTwoFactorAsync(string userId, string token)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            bool result = await VerifyMfaCode(user, token);

            if (!result)
            {
                return new Response<UserDto> { IsSuccessful = false };
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            return await PrepareSuccessfulLoginResponseAsync(user, true);
        }

        /// <summary>
        /// Generates a key for authentication
        /// </summary>
        /// <param name="userId">The user we want to generate a key for</param>
        /// <returns>A string that indicates will be equal to the key if the operation was successful or null if it failed</returns>
        public async Task<string> GenerateAuthenticatorKeyAsync(string userId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            string? key = await _userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);

                return await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return key;
        }

        /// <summary>
        /// Generates a new recovery code for two factor authentication option
        /// </summary>
        /// <param name="userId">The user we want to generate the new recovery code</param>
        /// <returns>An enumerable of type string containing the recovery codes</returns>
        public async Task<IEnumerable<string>> GenerateNewTwoFactorRecoveryCodesAsync(string userId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            return await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 6);
        }

        /// <summary>
        /// Generates a new refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token we want to generate</param>
        /// <param name="userDto">The user we want to generate a refresh token for</param>
        /// <returns>A response of type user dto containing the new refresh token</returns>
        public async Task<Response<UserDto>> GetNewTokensAsync(string refreshToken)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            RefreshToken? refreshTokenEntity = await _applicationDbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.ApplicationUserId == loggedInUser.Id);

            if (refreshTokenEntity == null || !refreshTokenEntity.IsActive)
            {
                return new Response<UserDto>
                {
                    IsSuccessful = false,
                    ErrorMessage = "Invalid refresh token"
                };
            }

            ApplicationUser? user = await _userManager.FindByIdAsync(refreshTokenEntity.ApplicationUserId);

            Response<UserDto>? response = await PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true, loggedInUser.SubscriptionId);

            refreshTokenEntity.RevokedOn = DateTime.UtcNow;
            refreshTokenEntity.ReplacedByToken = response.Dto.RefreshToken;
            await _applicationDbContext.SaveChangesAsync();

            return response;
        }

        /// <summary>
        /// Gets the recovery codes for a specific user
        /// </summary>
        /// <param name="userId">The user we want to get the recovery codes for</param>
        /// <returns>Returns the number of recovery codes that are still available for the user</returns>
        public async Task<int> GetRecoveryCodesNumberAsync(string userId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            return await _userManager.CountRecoveryCodesAsync(user);
        }

        /// <summary>
        /// Checks if the refresh token is active
        /// </summary>
        /// <param name="userId">The user id that we want to check if the refresh token is active</param>
        /// <param name="refreshToken">The refresh token we want to check if it is active</param>
        /// <returns>A bool representing if the refresh token is active or not</returns>
        public async Task<bool> IsRefreshTokenActiveAsync(string userId, string refreshToken)
        {
            RefreshToken? refreshTokenEntity = await _applicationDbContext.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.ApplicationUserId == userId && rt.Token == refreshToken);

            return refreshTokenEntity != null && refreshTokenEntity.IsActive;
        }

        /// <summary>
        /// Handles the action of login in
        /// </summary>
        /// <param name="loginDto">The login information we use to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<UserDto>> LoginAsync(LoginDto loginDto)
        {
            Response<UserDto>? response = new Response<UserDto>
            {
                IsSuccessful = false
            };

            string invalidCredentialsMessage = "Invalid credentials";

            ApplicationUser? user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                response.ErrorMessage = invalidCredentialsMessage;
                return response;
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                response.ErrorMessage = "You didn't confirm your email address";
                return response;
            }

            if (!await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                response.ErrorMessage = invalidCredentialsMessage;
                return response;
            }

            return await PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: false);
        }

        /// <summary>
        /// Handles the action of login with an Azure Account
        /// </summary>
        /// <param name="email">The email of the user that tries to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<UserDto>> LoginAzureAccountAsync(string email)
        {
            Response<UserDto>? response = new Response<UserDto>
            {
                IsSuccessful = false
            };

            string invalidCredentialsMessage = "Invalid credentials";

            ApplicationUser? user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                response.ErrorMessage = invalidCredentialsMessage;
                return response;
            }

            return await PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: false);
        }

        /// <summary>
        /// Handles the action of login in with an Azure Account on Shop App
        /// </summary>
        /// <param name="loginDto">The login information the user uses to login</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<SimpleUserDto>> LoginSimpleAzureAccountAsync(SimpleLoginDto loginDto)
        {
            Response<SimpleUserDto>? response = new Response<SimpleUserDto>
            {
                IsSuccessful = false
            };

            Guid? subscriptionId = (await _applicationDbContext.Subscriptions.FirstOrDefaultAsync(subscription => subscription.GraphConfig.Tenant == loginDto.TenantId))?.Id;

            if (subscriptionId == null)
            {
                return response;
            }

            SimpleUserDto? dto = new SimpleUserDto
            {
                Id = loginDto.Id,
                SubscriptionId = subscriptionId.Value,
                Name = loginDto.Name,
                Email = loginDto.Email,
            };

            dto.AccessToken = GenerateSimpleAccessToken(dto);

            response.IsSuccessful = true;
            response.Dto = dto;

            return response;
        }

        /// <summary>
        /// Logs the user in using a MFA code
        /// </summary>
        /// <param name="userId">The user we want to login</param>
        /// <param name="code">The code used to login the user</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<UserDto>> LoginMfaCodeAsync(string userId, string code)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            if (!await VerifyMfaCode(user, code))
            {
                return new Response<UserDto>
                {
                    IsSuccessful = false,
                    ErrorMessage = "Code invalid, please try again."
                };
            }

            return await PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true);
        }

        /// <summary>
        /// Handles the action of login in with a recovery code
        /// </summary>
        /// <param name="userId">The user id of the user that tries to login with a recovery code</param>
        /// <param name="code">The recovery code</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<UserDto>> LoginRecoveryCode(string userId, string code)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            string? formattedCode = code.Replace(" ", "").Replace("-", "");

            IdentityResult? result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, formattedCode);

            if (!result.Succeeded)
            {
                return new Response<UserDto>
                {
                    IsSuccessful = false,
                    ErrorMessage = "Code invalid, please try again."
                };
            }

            return await PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true);
        }

        /// <summary>
        /// Prepares the information for a successful login
        /// </summary>
        /// <param name="user">The user that tries to login</param>
        /// <param name="mfaCodeEntered">A bool that represent if the user entered a MFA code or not</param>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<Response<UserDto>> PrepareSuccessfulLoginResponseAsync(
            ApplicationUser user,
            bool mfaCodeEntered,
            Guid? subscriptionId = null)
        {
            Response<UserDto>? response = new Response<UserDto>
            {
                IsSuccessful = true,
            };

            response.Dto = _mapper.Map<UserDto>(user);

            if (user.IsSuperAdmin)
            {
                response.Dto.UserRole = UserRole.SuperAdmin;

                Subscription subscription = null;

                if (subscriptionId.HasValue)
                {
                    subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == subscriptionId);
                }
                else
                {
                    subscription = await _applicationDbContext.Subscriptions
                        .OrderBy(s => s.CreatedOn)
                        .FirstOrDefaultAsync();
                }

                if (subscription != null)
                {
                    response.Dto.SubscriptionId = subscription.Id;

                    response.Dto.Subscriptions = new List<UserSubscriptionDto>
                    {
                        _mapper.Map<UserSubscriptionDto>(subscription)
                    };
                }
            }
            else
            {
                DbSet<SubscriptionUser>? subUsers = _applicationDbContext.SubscriptionUsers;
                SubscriptionUser? subUser = null;

                if (subscriptionId.HasValue)
                {
                    subUser = await subUsers.SingleAsync(subUser =>
                        subUser.SubscriptionId == subscriptionId && subUser.ApplicationUserId == user.Id);
                }
                else
                {
                    subUser = await subUsers
                        .Where(subUser => subUser.ApplicationUserId == user.Id)
                        .OrderBy(subUser => subUser.CreatedOn)
                        .FirstOrDefaultAsync();
                }

                if (subUser != null)
                {
                    response.Dto.UserRole = subUser.UserRole;
                    response.Dto.SubscriptionId = subUser.SubscriptionId;

                    response.Dto.Subscriptions = await _applicationDbContext
                        .SubscriptionUsers
                        .Where(subUser => subUser.ApplicationUserId == user.Id)
                        .Include(subUser => subUser.Subscription)
                        .ProjectTo<UserSubscriptionDto>(_mapper.ConfigurationProvider)
                        .ToListAsync();
                }
            }

            response.Dto.AccessToken = GenerateAccessToken(response.Dto, user, mfaCodeEntered);

            RefreshToken? refreshToken = await CreateRefreshTokenAsync(user.Id);

            response.Dto.RefreshToken = refreshToken.Token;

            return response;
        }

        /// <summary>
        /// Handles the action of removing the old refresh token
        /// </summary>
        /// <returns>Void</returns>
        public async Task RemoveOldRefreshTokensAsync()
        {
            int numberOfDaysToKeepRefreshTokens = 2;

            IQueryable<RefreshToken>? oldRefreshTokens = _applicationDbContext.RefreshTokens
                .Where(rt =>
                    (rt.RevokedOn != null || rt.ExpiresOn <= DateTime.UtcNow) &&
                    rt.CreatedOn.AddDays(numberOfDaysToKeepRefreshTokens) <= DateTime.UtcNow);

            _applicationDbContext.RefreshTokens.RemoveRange(oldRefreshTokens);
            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Handles the action of reseting the password
        /// </summary>
        /// <param name="passwordResetDto">The password information we want to reset</param>
        /// <returns>A bool indicating if the action was successful or not</returns>
        public async Task<bool> ResetPasswordAsync(PasswordResetDto passwordResetDto)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(passwordResetDto.Email);
            if (user == null)
            {
                return false;
            }

            IdentityResult? result = await _userManager.ResetPasswordAsync(user, passwordResetDto.Token, passwordResetDto.NewPassword);

            return result.Succeeded;
        }

        /// <summary>
        /// Handles the action of revoking all refresh tokens of a user
        /// </summary>
        /// <param name="userId">The user id from which we want to revoke all refresh tokens</param>
        /// <returns>Void</returns>
        public async Task RevokeAllRefreshTokensForUserAsync(string userId)
        {
            List<RefreshToken>? refreshTokens = await _applicationDbContext.RefreshTokens
                .Where(rt => rt.ApplicationUserId == userId && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
                .ToListAsync();

            foreach (RefreshToken? refreshToken in refreshTokens)
            {
                refreshToken.RevokedOn = DateTime.UtcNow;
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Handles the action of revoking a token of a user
        /// </summary>
        /// <param name="userId">The user id from which we want to revoke a token</param>
        /// <param name="refreshToken">The refresh token we want to revoke</param>
        /// <returns>Void</returns>
        public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
        {
            RefreshToken? refreshTokenEntity = await _applicationDbContext.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.ApplicationUserId == userId && rt.Token == refreshToken);

            if (refreshTokenEntity?.RevokedOn == null)
            {
                refreshTokenEntity.RevokedOn = DateTime.UtcNow;
                await _applicationDbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sends an email containing the information for reseting the password
        /// </summary>
        /// <param name="email">The email we want to send the information about password reset</param>
        /// <returns>Void</returns>
        public async Task SendPasswordResetEmailAsync(string email)
        {
            string baseUrl = _envOptions.BaseUrl;
            ApplicationUser? user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return;
            }

            string? token = await _userManager.GeneratePasswordResetTokenAsync(user);

            string? tokenEncoded = HttpUtility.UrlEncode(token);
            string? emailEncoded = HttpUtility.UrlEncode(email);

            string? confirmLink = $"{baseUrl}/reset-password?token={tokenEncoded}&email={emailEncoded}";

            string? emailTemplate = EmailResources.NewPasswordTemplate;
            emailTemplate = emailTemplate.Replace("#templateFirstName", user.FirstName);
            emailTemplate = emailTemplate.Replace("#templateResetPassLink", confirmLink);
            emailTemplate = emailTemplate.Replace("#logoUrl", baseUrl);

            List<Attachment> attachments = new()
            {
                ApplicationHelper.GetImageAttachment(EmailResources.PassResetIcon, ImageFormat.Png, nameof(EmailResources.PassResetIcon)),
                ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo))
            };

            await _emailService.SendEmailAsync(new EmailDetailsDto
            {
                ToEmail = email,
                Subject = "Reset Password",
                HTMLContent = emailTemplate,
                Attachments = attachments
            });
        }

        /// <summary>
        /// Creates a new account on Endpoint Admin
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <returns>Application user containing information about the new created account</returns>
        private async Task<ApplicationUser> CreateAccountAsync(RegistrationDto registrationDto)
        {
            ApplicationUser applicationUser = await _userManager.FindByEmailAsync(registrationDto.Email);

            if (applicationUser == null)
            {
                applicationUser = _mapper.Map<ApplicationUser>(registrationDto);
                applicationUser.UserName = applicationUser.Email;
                applicationUser.TwoFactorRequired = true;
                applicationUser.LastAcceptedTermsVersion = _applicationInformation.TermsVersion;

                if (!_userManager.Users.Any())
                {
                    applicationUser.IsSuperAdmin = true;
                }

                IdentityResult userResult;

                if (!string.IsNullOrEmpty(registrationDto.Password))
                {
                    userResult = await _userManager.CreateAsync(applicationUser, registrationDto.Password);
                }
                else
                {
                    userResult = await _userManager.CreateAsync(applicationUser);

                    ApplicationUser? createdAppUser = await _userManager.FindByEmailAsync(registrationDto.Email);
                    createdAppUser.EmailConfirmed = true;
                    createdAppUser.TwoFactorRequired = false;

                    await _applicationDbContext.SaveChangesAsync();
                }

                if (userResult.Succeeded)
                {
                    return applicationUser;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates an Azure Account from an invitation link
        /// </summary>
        /// <param name="registrationDto">The information that the user inserted in order to create an account</param>
        /// <param name="userInvitation">The invitation send to the user that wants to create an account</param>
        /// <returns>Returns a response of type string indicating if the action was successful or not and with a message</returns>
        private async Task<Response<string>> CreateAzureAccountFromInvitationAsync(RegistrationDto registrationDto, UserInvitation userInvitation)
        {
            Response<string>? response = new Response<string>
            {
                IsSuccessful = false,
                ErrorMessage = "Registration with Azure AD failed! Invalid or expired invitation."
            };

            if (userInvitation.UserHasRegistered)
            {
                response.ErrorMessage = $"Email {registrationDto.Email} already is registered.";
                return response;
            }

            bool validInvitation = userInvitation.ModifiedOn.AddDays(AuthConstants.InvitationTokenExpirationIntervalDays) > DateTime.UtcNow;

            if (!validInvitation)
            {
                response.ErrorMessage = $"Could not create account for {registrationDto.Email}. Invitation expired.";
                return response;
            }

            ApplicationUser? existingAppUser = await _userManager.FindByEmailAsync(registrationDto.Email);

            if (existingAppUser == null)
            {
                ApplicationUser? newApplicationUser = await CreateAccountAsync(registrationDto);

                if (newApplicationUser != null)
                {
                    response.IsSuccessful = true;
                }

                response.ErrorMessage = response.IsSuccessful ? null : "Registration with Azure AD failed! Could not create account.";

                if (!response.IsSuccessful)
                {
                    return response;
                }

                ApplicationUser? createdAppUser = await _userManager.FindByEmailAsync(registrationDto.Email);

                await _applicationDbContext.SubscriptionUsers.AddAsync(new SubscriptionUser
                {
                    ApplicationUserId = createdAppUser.Id,
                    SubscriptionId = userInvitation.SubscriptionId,
                    UserRole = userInvitation.UserRole
                });

                userInvitation.UserHasRegistered = true;

                response.IsSuccessful = true;

                await _applicationDbContext.SaveChangesAsync();
            }

            return response;
        }

        /// <summary>
        /// Creates the payment information
        /// </summary>
        /// <param name="registrationDto">The registration information the user uses</param>
        /// <param name="subscription">The subscription for which the payment information was created</param>
        /// <returns>A response of type farpay result containing information about the payment</returns>
        private async Task<Response<FarPayResult>> CreatePaymentInformation(RegistrationDto registrationDto, Subscription subscription)
        {
            string fullName = $"{registrationDto.FirstName} {registrationDto.LastName}";

            Response<FarPayResult>? result = new Response<FarPayResult>
            {
                ErrorMessage = $"Could not create payment account for {registrationDto.Email}.",
                IsSuccessful = false
            };

            EconomicCustomerDto? economicDto = _economicService.GetEconomicCustomerDto(registrationDto);

            Response<EconomicCustomerDto>? createEconomicCustomerResponse = await _economicService.CreateEconomicCustomerAsync(economicDto);

            if (!createEconomicCustomerResponse.IsSuccessful)
            {
                result.ErrorMessage = createEconomicCustomerResponse.ErrorMessage;
                return result;
            }

            if (!_applicationInformation.AskForPaymentAfterRegister)
            {
                return new Response<FarPayResult> { IsSuccessful = true };
            }

            FarPayCustomerDto? farPayCustomer = _farPayService.GetFarPayCustomerDto(createEconomicCustomerResponse.Dto.CustomerNumber, registrationDto);

            Response<FarPayCustomerDto>? createFarPayCustomerResponse = await _farPayService.CreateCustomerAsync(farPayCustomer);

            if (!createFarPayCustomerResponse.IsSuccessful)
            {
                result.ErrorMessage = createFarPayCustomerResponse.ErrorMessage;
                return result;
            }

            FarPayOrderDto? farPayOrder = _farPayService.GetFarPayOrderDto(createEconomicCustomerResponse.Dto.CustomerNumber, subscription.Id);

            Response<FarPayResult>? orderResult = await _farPayService.CreateOrderAsync(farPayOrder);

            if (orderResult.IsSuccessful)
            {
                subscription.FarPayToken = orderResult.Dto.Token;
                subscription.CustomerNumber = farPayOrder.Customer.CustomerNumber;
            }

            await _applicationDbContext.SaveChangesAsync();

            return orderResult;
        }

        /// <summary>
        /// Creates a refresh token
        /// </summary>
        /// <param name="userId">The user id for which we want to create a refresh token</param>
        /// <returns>A refresh token entity containing the user id and an expiration time stamp and a token</returns>
        private async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            RefreshToken? refreshToken = new RefreshToken
            {
                ApplicationUserId = userId,
                Token = TokenHelper.GetRandomTokenString(),
                ExpiresOn = DateTime.UtcNow.AddDays(1)
            };

            _applicationDbContext.RefreshTokens.Add(refreshToken);
            await _applicationDbContext.SaveChangesAsync();

            return refreshToken;
        }

        /// <summary>
        /// Creates a subscription
        /// </summary>
        /// <param name="registrationDto">The registration information the user uses to create a subscription</param>
        /// <param name="applicationUser">The user that created the subscription</param>
        /// <returns>The new subscription entity that was created</returns>
        private async Task<Subscription> CreateSubscriptionAsync(RegistrationDto registrationDto, ApplicationUser applicationUser)
        {
            string? subscriptionName = registrationDto.CompanyName;

            int nrOfSubNames = await _applicationDbContext.Subscriptions
                .Where(s => s.CompanyName == registrationDto.CompanyName)
                .CountAsync();

            if (nrOfSubNames > 0)
            {
                subscriptionName = $"{subscriptionName} {nrOfSubNames + 1}";
            }

            Subscription subscription = _mapper.Map<Subscription>(registrationDto);
            subscription.Name = subscriptionName;
            subscription.Email = registrationDto.SubscriptionEmail;
            subscription.LogoSmall = string.Empty;

            if (_userManager.Users.Count() == 1)
            {
                subscription.State = SubscriptionState.Testing;
            }
            else
            {
                subscription.State = SubscriptionState.PaymentNotSetUp;
            }

            await _applicationDbContext.Subscriptions.AddAsync(subscription);

            await _applicationDbContext.SaveChangesAsync();

            if (_userManager.Users.Count() != 1)
            {
                SubscriptionUser? subscriptionUser = new SubscriptionUser
                {
                    SubscriptionId = subscription.Id,
                    ApplicationUserId = applicationUser.Id,
                    UserRole = UserRole.Administrator
                };

                await _applicationDbContext.SubscriptionUsers.AddAsync(subscriptionUser);
            }

            await _applicationDbContext.SaveChangesAsync();

            return subscription;
        }

        /// <summary>
        /// Generates an access token
        /// </summary>
        /// <param name="userDto">The user information</param>
        /// <param name="applicationUser">The user for which the access token was created</param>
        /// <param name="mfaCodeEntered">A bool that represent if the user entered a MFA code or not</param>
        /// <returns>A string representing the token generated</returns>
        private string GenerateAccessToken(UserDto userDto, ApplicationUser applicationUser, bool mfaCodeEntered)
        {
            HashSet<Claim>? claims = new HashSet<Claim>
            {
                new(ClaimTypes.NameIdentifier, userDto.Id),
                new(HorizonClaimTypes.Mfa,
                    HorizonClaimTypes.MfaClaimValues.GetValue(
                        applicationUser.TwoFactorRequired,
                        applicationUser.TwoFactorEnabled,
                        mfaCodeEntered))
            };

            if (userDto.UserRole != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, userDto.UserRole));
            }

            if (userDto.SubscriptionId != null)
            {
                claims.Add(new Claim(HorizonClaimTypes.SubscriptionId, userDto.SubscriptionId.ToString()));
            }

            JwtSecurityTokenHandler? jwtTokenHandler = new JwtSecurityTokenHandler();

            SymmetricSecurityKey? securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthentication.SecretKey));
            SigningCredentials? signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken? jwtToken = jwtTokenHandler.CreateJwtSecurityToken(
                issuer: _jwtAuthentication.Issuer,
                audience: _jwtAuthentication.Audience,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: signingCredentials,
                subject: new ClaimsIdentity(claims)
            );

            return jwtTokenHandler.WriteToken(jwtToken);
        }

        /// <summary>
        /// Generates a simple a access token for users of Shop App
        /// </summary>
        /// <param name="userDto">The user for which we want to generate a simple access token</param>
        /// <returns>A string representing the token</returns>
        private string GenerateSimpleAccessToken(SimpleUserDto userDto)
        {
            HashSet<Claim>? claims = new HashSet<Claim>
            {
                new (ClaimTypes.NameIdentifier, userDto.Id),
                new (ClaimTypes.Upn, userDto.Email),
                new (ClaimConstants.Name, userDto.Name),
                new (HorizonClaimTypes.SubscriptionId, userDto.SubscriptionId.ToString())
            };

            JwtSecurityTokenHandler? jwtTokenHandler = new JwtSecurityTokenHandler();

            SymmetricSecurityKey? securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthentication.SecretKey));
            SigningCredentials? signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken? jwtToken = jwtTokenHandler.CreateJwtSecurityToken(
                issuer: _jwtAuthentication.Issuer,
                audience: _jwtAuthentication.Audience,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: signingCredentials,
                subject: new ClaimsIdentity(claims)
            );

            return jwtTokenHandler.WriteToken(jwtToken);
        }

        /// <summary>
        /// Sends a confirmation email when creating a new account or changing the email
        /// </summary>
        /// <returns>Void</returns>
        private async Task SendConfirmEmailAsync(ApplicationUser applicationUser)
        {
            string baseUrl = _envOptions.BaseUrl;
            string? token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);

            string? tokenEncoded = HttpUtility.UrlEncode(token);
            string? emailEncoded = HttpUtility.UrlEncode(applicationUser.Email);

            string? confirmLink = $"{baseUrl}/confirm-email?token={tokenEncoded}&email={emailEncoded}";

            string? emailTemplate = EmailResources.NewSubscriptionTemplate;
            emailTemplate = emailTemplate.Replace("#templateFirstName", applicationUser.FirstName);
            emailTemplate = emailTemplate.Replace("#templateActivationUrl", confirmLink);
            emailTemplate = emailTemplate.Replace("#logoUrl", baseUrl);

            List<Attachment> attachments = new()
            {
                ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo)),
                ApplicationHelper.GetImageAttachment(EmailResources.SubActivationIcon, ImageFormat.Png, nameof(EmailResources.SubActivationIcon)),
                ApplicationHelper.GetImageAttachment(EmailResources.SubGetStartedIcon, ImageFormat.Png, nameof(EmailResources.SubGetStartedIcon))
            };

            await _emailService.SendEmailAsync(new EmailDetailsDto
            {
                ToEmail = applicationUser.Email,
                ToName = applicationUser.FullName,
                Subject = "Welcome! Confirm your email address",
                HTMLContent = emailTemplate,
                Attachments = attachments
            });
        }

        /// <summary>
        /// Verifies if the MFA code entered is valid
        /// </summary>
        /// <param name="user">The user for which we want to verify the code</param>
        /// <param name="code">The MFA code we want to verify</param>
        /// <returns>A bool determining if the code is valid or not</returns>
        private async Task<bool> VerifyMfaCode(ApplicationUser user, string code)
        {
            string? formattedCode = code.Replace(" ", "").Replace("-", "");

            return await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                formattedCode);
        }
    }
}
