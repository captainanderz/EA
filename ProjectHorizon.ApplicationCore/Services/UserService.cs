using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Properties;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly ApplicationInformation _applicationInformation;
        private readonly Options.Environment _envOptions;
        private readonly IHubContext<SignalRHub> _hubContext;

        public UserService(
            IApplicationDbContext applicationDbContext,
            IAzureBlobService azureBlobService,
            IEmailService emailService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IOptions<ApplicationInformation> applicationInformation,
            IAuthService authService,
            IAuditLogService auditLogService,
            ILoggedInUserProvider loggedInUserProvider,
            IOptions<Options.Environment> envOptions, 
            Microsoft.AspNetCore.SignalR.IHubContext<SignalRHub> hubContext)
        {
            _applicationDbContext = applicationDbContext;
            _azureBlobService = azureBlobService;
            _emailService = emailService;
            _mapper = mapper;
            _userManager = userManager;
            _authService = authService;
            _auditLogService = auditLogService;
            _loggedInUserProvider = loggedInUserProvider;
            _applicationInformation = applicationInformation.Value;
            _envOptions = envOptions.Value;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Gets the user based on his user id
        /// </summary>
        /// <param name="userId">The id of the user we want to get</param>
        /// <returns>The user that has the given id</returns>
        public async Task<UserDto> GetAsync(string userId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            Response<UserDto>? response = await _authService.PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true);

            return response.Dto;
        }

        /// <summary>
        /// Gets all super admin users
        /// </summary>
        /// <returns>An enumerable with all super admin users</returns>
        public async Task<IEnumerable<UserDto>> GetSuperAdminUsersAsync()
            => await _applicationDbContext
                .Users
                .Where(user => user.IsSuperAdmin)
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Select(user => new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    ProfilePictureSmall = user.ProfilePictureSmall,
                    TwoFactorRequired = user.TwoFactorRequired,
                })
                .ToListAsync();

        /// <summary>
        /// Disables the two factor authentication option for a user
        /// </summary>
        /// <param name="id">The id of the user we want to disable the two factor authentication option</param>
        /// <returns>Void</returns>
        public async Task ToggleTwoFactorAuthenticationAsync(string id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            user.TwoFactorRequired = !user.TwoFactorRequired;
            await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Changes the role of a user to super admin user
        /// </summary>
        /// <param name="id">The id of the user we want to make super admin</param>
        /// <returns>Void</returns>
        public async Task MakeUserSuperAdminAsync(string id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            user.IsSuperAdmin = true;
            await _userManager.UpdateAsync(user);

            List<SubscriptionUser>? userSubscriptions = _applicationDbContext
                .SubscriptionUsers
                .Where(su => su.ApplicationUserId == user.Id)
                .ToList();

            _applicationDbContext.SubscriptionUsers.RemoveRange(userSubscriptions);
            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task RemoveUserSuperAdminAsync(string id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            user.IsSuperAdmin = false;
            await _userManager.UpdateAsync(user);
            await _authService.RevokeAllRefreshTokensForUserAsync(id);
            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a new account by accessing the invitation to a subscription by an admin or a super admin user
        /// </summary>
        /// <param name="registerInvitationDto">The invite registration we send to the user we want to register by invitation</param>
        /// <returns>A response of type register invitation to containing information about a new account registration</returns>
        public async Task<Response<RegisterInvitationDto>> RegisterInvitationAsync(RegisterInvitationDto registerInvitationDto)
        {
            string subscriptionName = HttpUtility.UrlDecode(registerInvitationDto.SubscriptionName);

            Response<RegisterInvitationDto>? registerInvitationResponse = new Response<RegisterInvitationDto>
            {
                IsSuccessful = false,
                ErrorMessage = "Registration failed! Invalid or expired invitation."
            };

            UserInvitation? userInvitation = await _applicationDbContext.UserInvitations
                .SingleOrDefaultAsync(x => x.Email == registerInvitationDto.Email && x.Subscription.Name == subscriptionName);

            if (userInvitation == null)
            {
                return registerInvitationResponse;
            }

            if (userInvitation.UserHasRegistered)
            {
                registerInvitationResponse.Dto = registerInvitationDto;
                registerInvitationResponse.ErrorMessage = $"Email {registerInvitationDto.Email} is already registered.";
                return registerInvitationResponse;
            }

            ApplicationUser? existingAppUser = await _userManager.FindByEmailAsync(registerInvitationDto.Email);

            if (existingAppUser == null &&
                userInvitation.ModifiedOn.AddDays(AuthConstants.InvitationTokenExpirationIntervalDays) > DateTime.UtcNow &&
                userInvitation.InvitationToken == registerInvitationDto.EmailToken &&
                !userInvitation.UserHasRegistered)
            {
                ApplicationUser applicationUser = new ApplicationUser()
                {
                    UserName = registerInvitationDto.Email,
                    FirstName = userInvitation.FirstName,
                    LastName = userInvitation.LastName,
                    Email = registerInvitationDto.Email,
                    EmailConfirmed = true,
                    TwoFactorRequired = true,
                    LastAcceptedTermsVersion = _applicationInformation.TermsVersion
                };
                await _userManager.CreateAsync(applicationUser, registerInvitationDto.Password);

                ApplicationUser? createdAppUser = await _userManager.FindByEmailAsync(registerInvitationDto.Email);

                await _applicationDbContext.SubscriptionUsers.AddAsync(new SubscriptionUser
                {
                    ApplicationUserId = createdAppUser.Id,
                    SubscriptionId = userInvitation.SubscriptionId,
                    UserRole = userInvitation.UserRole
                });

                registerInvitationResponse.IsSuccessful = true;
                registerInvitationResponse.ErrorMessage = null;

                userInvitation.UserHasRegistered = true;

                await _applicationDbContext.SaveChangesAsync();
            }

            return registerInvitationResponse;
        }

        /// <summary>
        /// Checks if the user invited in the subscription already has an account
        /// </summary>
        /// <param name="registerInvitationDto">The invite registration we send to the user we want to register by invitation</param>
        /// <returns>A bool determining if the user already have an account or not</returns>
        public async Task<bool> IsInvitedUserAlreadyRegistered(InvitedUserDto registerInvitationDto)
        {
            string subscriptionName = HttpUtility.UrlDecode(registerInvitationDto.SubscriptionName);
            UserInvitation? userInvitation = await _applicationDbContext.UserInvitations
                .SingleOrDefaultAsync(x => x.Email == registerInvitationDto.Email && x.Subscription.Name == subscriptionName);

            return userInvitation.UserHasRegistered;
        }

        /// <summary>
        /// Invite a new user to a subscription by invitation
        /// </summary>
        /// <param name="userInvitationDto">The invitation sent to the user</param>
        /// <param name="loggedInUser">The user currently logged in</param>
        /// <returns>Void</returns>
        public async Task InviteUserAsync(UserInvitationDto userInvitationDto, UserDto loggedInUser)
        {
            UserRole.ValidateUserRole(userInvitationDto.UserRole);

            (ApplicationUser invitedApplicationUser, string emailToken) = await AddOrUpdateInvitationAsync(userInvitationDto, loggedInUser);

            await AddInvitedUserToSubscriptionAsync(loggedInUser, invitedApplicationUser.Email);

            await SendRegistrationEmailAsync(loggedInUser, invitedApplicationUser, emailToken);
        }

        /// <summary>
        /// Changes the current subscription
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription we want to change</param>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<UserDto> ChangeSubscriptionAsync(Guid subscriptionId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(_loggedInUserProvider.GetLoggedInUser().Id);

            Response<UserDto>? response = await _authService.PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true, subscriptionId);

            return response.Dto;
        }

        /// <summary>
        /// Reloads the current subscription
        /// </summary>
        /// <returns>A response with the user information that wants to login</returns>
        public async Task<UserDto> ReloadSubscriptionAsync()
        {
            UserDto? userDto = _loggedInUserProvider.GetLoggedInUser();

            ApplicationUser? user = await _userManager.FindByIdAsync(userDto.Id);

            Response<UserDto>? response = await _authService.PrepareSuccessfulLoginResponseAsync(user, mfaCodeEntered: true, userDto.SubscriptionId);

            return response.Dto;
        }

        /// <summary>
        /// Changes the password of an account
        /// </summary>
        /// <param name="userId">The id of the user we want to change the password for</param>
        /// <param name="dto">The information about the new password that was changed</param>
        /// <returns>An identity result containing the new changed password for the account of the given user</returns>
        public async Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            return await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        }

        /// <summary>
        /// Makes the invited user part of the subscription he was invited to
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="userEmail">The email of the user that was invited</param>
        /// <returns>Void</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task AddInvitedUserToSubscriptionAsync(UserDto loggedInUser, string userEmail)
        {
            ApplicationUser? existingAppUser = await _userManager.FindByEmailAsync(userEmail);

            if (existingAppUser == null)
            {
                return;
            }

            SubscriptionUser? existingUserAlreadyOnSubscription = await _applicationDbContext
                .SubscriptionUsers
                .SingleOrDefaultAsync(x => x.SubscriptionId == loggedInUser.SubscriptionId && x.ApplicationUserId == existingAppUser.Id);

            UserInvitation? userInvitation = await _applicationDbContext
                .UserInvitations
                .SingleAsync(x => x.Email == userEmail && x.SubscriptionId == loggedInUser.SubscriptionId);

            userInvitation.UserHasRegistered = true;

            if (existingUserAlreadyOnSubscription != null)
            {
                await _applicationDbContext.SaveChangesAsync();
                throw new InvalidOperationException($"{existingAppUser.FullName} is already on this subscription");
            }

            _applicationDbContext.SubscriptionUsers.Add(new SubscriptionUser
            {
                ApplicationUserId = existingAppUser.Id,
                SubscriptionId = userInvitation.SubscriptionId,
                UserRole = userInvitation.UserRole
            });

            await _applicationDbContext.SaveChangesAsync();

            await _hubContext.Clients.User(existingAppUser.Id).SendAsync(SignalRMessages.UserUpdate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userInvitationDto"></param>
        /// <param name="loggedInUser"></param>
        /// <returns></returns>
        private async Task<(ApplicationUser, string)> AddOrUpdateInvitationAsync(UserInvitationDto userInvitationDto, UserDto loggedInUser)
        {
            UserInvitation? existingUserInvitation = await _applicationDbContext.UserInvitations
                .SingleOrDefaultAsync(x =>
                    x.Email == userInvitationDto.Email &&
                    x.SubscriptionId == loggedInUser.SubscriptionId);

            (ApplicationUser invitedApplicationUser, string emailToken) = await GetUserInfoAsync(userInvitationDto);

            if (existingUserInvitation != null)
            {
                existingUserInvitation.FirstName = userInvitationDto.FirstName;
                existingUserInvitation.LastName = userInvitationDto.LastName;
                existingUserInvitation.UserRole = userInvitationDto.UserRole;
                existingUserInvitation.InvitationToken = emailToken;
            }
            else
            {
                UserInvitation userInvitation = _mapper.Map<UserInvitation>(userInvitationDto);

                userInvitation.SubscriptionId = loggedInUser.SubscriptionId;
                userInvitation.ApplicationUserId = loggedInUser.Id;
                userInvitation.InvitationToken = emailToken;
                _applicationDbContext.UserInvitations.Add(userInvitation);
            }

            await _applicationDbContext.SaveChangesAsync();

            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == loggedInUser.SubscriptionId);
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.UserManagement,
                string.Format(AuditLogActions.UserInvitation, userInvitationDto.Email, subscription.Name),
                author: loggedInUser);

            return (invitedApplicationUser, emailToken);
        }

        /// <summary>
        /// Sends an email to a user we want to invite in a given subscription
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="invitedApplicationUser">The user we want to invite in a subscription</param>
        /// <param name="emailToken">The email we encode the invitation token in</param>
        /// <returns>Void</returns>
        private async Task SendRegistrationEmailAsync(UserDto loggedInUser, ApplicationUser invitedApplicationUser, string emailToken)
        {
            string baseUrl = _envOptions.BaseUrl;
            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(invitedApplicationUser.Email);

            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == loggedInUser.SubscriptionId);

            string tokenEncoded = HttpUtility.UrlEncode(emailToken);
            string emailEncoded = HttpUtility.UrlEncode(invitedApplicationUser.Email);
            string subscriptionNameEncoded = HttpUtility.UrlEncode(subscription.Name);

            string? invitedUserFullName = $"{invitedApplicationUser.FirstName} {invitedApplicationUser.LastName}";

            ApplicationUser? currentUser = await _userManager.FindByIdAsync(loggedInUser.Id);

            EmailDetailsDto emailDetailsDto = new EmailDetailsDto
            {
                ToEmail = invitedApplicationUser.Email,
                ToName = invitedUserFullName,
            };

            string? emailTemplate = EmailResources.SubscriptionInviteTemplate;
            List<Attachment>? attachments = new List<Attachment>();
            attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo)));
            attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.AddedSubIcon, ImageFormat.Png, nameof(EmailResources.AddedSubIcon)));
            emailTemplate = emailTemplate.Replace("#logoUrl", baseUrl);

            if (existingUser == null)
            {
                string? invitationLink = $"{baseUrl}/register-invitation?token={tokenEncoded}&email={emailEncoded}&subname={subscriptionNameEncoded}";
                string invitationText = $"{currentUser.FullName} has invited you to their {subscription.Name} subscription on Endpoint Admin. " +
                    $"You just need to activate your account choosing a password by clicking on the link below.";

                emailTemplate = emailTemplate.Replace("#templateFirstName", invitedApplicationUser.FirstName);
                emailTemplate = emailTemplate.Replace("#invitationText", invitationText);
                emailTemplate = emailTemplate.Replace("#activateInvitationLink", invitationLink);
                emailTemplate = emailTemplate.Replace("#invitationLinkText", "Activate");

                emailDetailsDto.Subject = "Welcome! Your Endpoint Admin invitation";
                emailDetailsDto.HTMLContent = emailTemplate;
            }
            else
            {
                string? loginLink = $"{baseUrl}/login";

                string addedMessage = $"You have been added to the Endpoint Admin subscription {subscription.Name} by {currentUser.FullName}. " +
                    $"Log in to check your subscription by clicking on the link below.";

                emailTemplate = emailTemplate.Replace("#templateFirstName", invitedApplicationUser.FirstName);
                emailTemplate = emailTemplate.Replace("#invitationText", addedMessage);
                emailTemplate = emailTemplate.Replace("#activateInvitationLink", loginLink);
                emailTemplate = emailTemplate.Replace("#invitationLinkText", "Login");

                emailDetailsDto.Subject = "Endpoint Admin - added to new subscription";
                emailDetailsDto.HTMLContent = emailTemplate;
            }

            emailDetailsDto.Attachments = attachments;

            await _emailService.SendEmailAsync(emailDetailsDto);
        }

        /// <summary>
        /// Changes the profile picture of the current user
        /// </summary>
        /// <param name="userId">The id of the user we want to change the profile picture for</param>
        /// <param name="picture">The picture we want to change</param>
        /// <returns>Void</returns>
        public async Task ChangeProfilePictureAsync(string userId, MemoryStream picture)
        {
            using TransactionScope? scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            user.ProfilePictureSmall = await ApplicationHelper.GetSmallPictureAsBase64Async(picture);
            await _userManager.UpdateAsync(user);

            await _azureBlobService.UploadProfilePictureAsync(userId, picture);

            scope.Complete();
        }

        /// <summary>
        /// Changes the role of a user 
        /// </summary>
        /// <param name="bulkChangeUsersRoleDto">A dto containing the user roles of more than one users</param>
        /// <returns>Void</returns>
        public async Task ChangeUsersRoleAsync(BulkChangeUsersRoleDto bulkChangeUsersRoleDto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            List<SubscriptionUser>? subUsers = await _applicationDbContext
                .SubscriptionUsers
                .Where(subUser =>
                    subUser.SubscriptionId == loggedInUser.SubscriptionId &&
                    bulkChangeUsersRoleDto.UserIds.Contains(subUser.ApplicationUserId))
                .ToListAsync();

            foreach (SubscriptionUser? subUser in subUsers)
            {
                subUser.UserRole = bulkChangeUsersRoleDto.NewUserRole;
                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.UserManagement,
                    string.Format(AuditLogActions.UserChangeRole, subUser.ApplicationUser.UserName, subUser.UserRole),
                    author: loggedInUser,
                    saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Finds a specific user based on the subscription he is part of
        /// </summary>
        /// <returns>An enumerable with all the users in a given subscription</returns>
        public async Task<IEnumerable<UserDto>> FindUsersBySubscriptionAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            return await _applicationDbContext
                  .SubscriptionUsers
                  .Where(subUser => subUser.SubscriptionId == loggedInUser.SubscriptionId)
                  .Select(subUser => new UserDto
                  {
                      Id = subUser.ApplicationUserId,
                      Email = subUser.ApplicationUser.Email,
                      FirstName = subUser.ApplicationUser.FirstName,
                      LastName = subUser.ApplicationUser.LastName,
                      UserRole = subUser.UserRole,
                      ProfilePictureSmall = subUser.ApplicationUser.ProfilePictureSmall,
                      TwoFactorRequired = subUser.ApplicationUser.TwoFactorRequired
                  })
                  .OrderBy(user => user.FirstName)
                  .ThenBy(user => user.LastName)
                  .ToListAsync();
        }

        /// <summary>
        /// Removes users from a given subscription
        /// </summary>
        /// <param name="userIds">The ids of the users we want to remove</param>
        /// <returns>Void</returns>
        public async Task RemoveUsersBySubscriptionAsync(IEnumerable<string> userIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            await GenerateAuditLogsForRemovedUsersAsync(loggedInUser, userIds);

            string? subscriptionIdParam = $@"{{{userIds.Count()}}}";
            string? userIdsParam = string.Join(",", userIds.Select((_, i) => "{" + i + "}"));
            string? subscriptionIdValue = loggedInUser.SubscriptionId.ToString();
            object[]? parameterValues = userIds.Append(subscriptionIdValue).ToArray<object>();

            using TransactionScope? transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            string? sqlQueryDelete = @"DELETE FROM [{0}] WHERE ([SubscriptionId] = {1}) AND ([ApplicationUserId] IN ({2}));";

            string? sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.Notifications), subscriptionIdParam, userIdsParam);
            await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

            sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.NotificationSettings), subscriptionIdParam, userIdsParam);
            await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

            sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.SubscriptionUsers), subscriptionIdParam, userIdsParam);
            await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

            foreach (string? userId in userIds)
            {
                bool hasSubscription = await _applicationDbContext.SubscriptionUsers.AnyAsync(subscriptionUser => subscriptionUser.ApplicationUserId == userId);
                
                if (!hasSubscription)
                {     
                    await _authService.RevokeAllRefreshTokensForUserAsync(userId);
                }
            }

            transactionScope.Complete();

            await _hubContext.Clients.Users(userIds.ToArray()).SendAsync(SignalRMessages.UserUpdate);
        }

        /// <summary>
        /// Deletes the profile picture of a specific user
        /// </summary>
        /// <param name="userId">The id of the user we want to delete the profile picture</param>
        /// <returns>Void</returns>
        public async Task DeleteProfilePictureAsync(string userId)
        {
            using TransactionScope? scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            user.ProfilePictureSmall = string.Empty;
            await _userManager.UpdateAsync(user);

            await _azureBlobService.RemoveProfilePictureAsync(userId);
            scope.Complete();
        }

        /// <summary>
        /// Changes the user settings
        /// </summary>
        /// <param name="userId">The id of the user we want to change settings for</param>
        /// <param name="dto">The user settings dto containing the change settings information</param>
        /// <returns></returns>
        public async Task<bool> ChangeSettingsAsync(string userId, UserSettingsDto dto)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);

            bool isNewEmailDifferentFromOld = user.Email != dto.Email;
            bool isNewEmailUnique = false;

            if (isNewEmailDifferentFromOld)
            {
                isNewEmailUnique = await _userManager.FindByEmailAsync(dto.Email) == null;
                if (isNewEmailUnique)
                {
                    user.NewUnconfirmedEmail = dto.Email;
                }
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            await _userManager.UpdateAsync(user);

            if (isNewEmailDifferentFromOld && isNewEmailUnique)
            {
                await SendChangeEmailAsync(user, dto.Email);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sends an email to the user asking him to confirm the action of changing his email
        /// </summary>
        /// <param name="user">The user that changed his email</param>
        /// <param name="newEmail">The new email of the user</param>
        /// <returns>Void</returns>
        private async Task SendChangeEmailAsync(ApplicationUser user, string newEmail)
        {
            string baseUrl = _envOptions.BaseUrl;
            string? token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);

            string? tokenEncoded = HttpUtility.UrlEncode(token);
            string? emailEncoded = HttpUtility.UrlEncode(user.Email);

            string? confirmLink = $"{baseUrl}/confirm-change-email?token={tokenEncoded}&email={emailEncoded}";

            string? emailTemplate = EmailResources.ChangeEmailTemplate;
            emailTemplate = emailTemplate.Replace("#templateFirstName", user.FirstName);
            emailTemplate = emailTemplate.Replace("#templateChangeEmailLink", confirmLink);
            emailTemplate = emailTemplate.Replace("#logoUrl", baseUrl);

            List<Attachment> attachments = new()
            {
                ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo)),
                ApplicationHelper.GetImageAttachment(EmailResources.SuccessIcon, ImageFormat.Png, nameof(EmailResources.SuccessIcon))
            };

            await _emailService.SendEmailAsync(new()
            {
                ToEmail = newEmail,
                ToName = user.FullName,
                Subject = "Welcome! Confirm your new email address",
                HTMLContent = emailTemplate,
                Attachments = attachments
            });
        }

        /// <summary>
        /// Generates audit log notifications for removing users
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="userIds">The ids of the users we want to remove</param>
        /// <returns>Void</returns>
        private async Task GenerateAuditLogsForRemovedUsersAsync(UserDto loggedInUser, IEnumerable<string> userIds)
        {
            List<SubscriptionUser>? subUsers = await _applicationDbContext.SubscriptionUsers
                .Where(subUser =>
                    subUser.SubscriptionId == loggedInUser.SubscriptionId &&
                    userIds.Contains(subUser.ApplicationUserId))
                .ToListAsync();

            foreach (SubscriptionUser? subUser in subUsers)
            {
                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.UserManagement,
                    string.Format(AuditLogActions.UserRemoved, subUser.ApplicationUser.UserName),
                    author: loggedInUser,
                    saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the information about the invited user
        /// </summary>
        /// <param name="userInvitationDto">The dto containing the information about the invited user</param>
        /// <returns>The new user entity and his token</returns>
        public async Task<(ApplicationUser, string)> GetUserInfoAsync(UserInvitationDto userInvitationDto)
        {
            ApplicationUser invitedApplicationUser = _mapper.Map<ApplicationUser>(userInvitationDto);
            invitedApplicationUser.UserName = invitedApplicationUser.Email;

            string token = await _userManager.GenerateEmailConfirmationTokenAsync(invitedApplicationUser);

            return (invitedApplicationUser, token);
        }

        //private async Task DeleteUsersAsync(IEnumerable<string> userIds)
        //{
        //    UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
        //    List<ApplicationUser> usersToDelete = new List<ApplicationUser>();

        //    string? subscriptionIdValue = loggedInUser.SubscriptionId.ToString();
        //    object[]? parameterValues = userIds.Append(subscriptionIdValue).ToArray<object>();

        //    using TransactionScope? transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        //    string? sqlQueryDelete = @"DELETE FROM [{0}] WHERE ([ApplicationUserId] IN ({1}));";

        //    string? sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.Notifications), userIds);
        //    await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

        //    sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.NotificationSettings), userIds);
        //    await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

        //    sql = string.Format(sqlQueryDelete, nameof(_applicationDbContext.SubscriptionUsers), userIds);
        //    await _applicationDbContext.Database.ExecuteSqlRawAsync(sql, parameterValues);

        //    foreach (string userId in userIds)
        //    {
        //        ApplicationUser user = await _applicationDbContext.Users.Where(user => user.Id== userId).FirstOrDefaultAsync();

        //        if (user != null)
        //        {
        //            usersToDelete.Add(user);
        //        }
        //    }

        //    transactionScope.Complete();

        //    _applicationDbContext.Users.RemoveRange(usersToDelete);
        //    await _applicationDbContext.SaveChangesAsync();
        //}
    }
}