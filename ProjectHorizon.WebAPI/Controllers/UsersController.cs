using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : HorizonBaseController
    {
        private readonly IUserService _userService;
        private readonly IAzureBlobService _azureBlobService;

        public UsersController(IUserService userService, IAzureBlobService azureBlobService)
        {
            _userService = userService;
            _azureBlobService = azureBlobService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            UserDto? response = await _userService.GetAsync(loggedInUser.Id);

            return Ok(response);
        }

        [HttpGet("superadmin-users")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [Authorize(Roles = UserRole.SuperAdmin)]
        public async Task<IActionResult> GetSuperAdminUsers()
        {
            return Ok(await _userService.GetSuperAdminUsersAsync());
        }

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeSubscription([FromBody][Required] Guid subscriptionId)
        {
            UserDto? newUserDto = await _userService.ChangeSubscriptionAsync(subscriptionId);

            SetRefreshTokenCookie(newUserDto.RefreshToken);

            return Ok(newUserDto);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReloadSubscription()
        {
            UserDto? userDto = await _userService.ReloadSubscriptionAsync();

            SetRefreshTokenCookie(userDto.RefreshToken);

            return Ok(userDto);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        public async Task<IActionResult> ListBySubscription()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            IEnumerable<UserDto>? users = await _userService.FindUsersBySubscriptionAsync();

            return Ok(users);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveBySubscription([FromBody] IEnumerable<string> userIds)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            if (userIds.Contains(loggedInUser.Id))
            {
                return Forbid();
            }

            await _userService.RemoveUsersBySubscriptionAsync(userIds);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPatch("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeRole([FromBody] BulkChangeUsersRoleDto bulkChangeUsersRoleDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            if (bulkChangeUsersRoleDto.UserIds.Contains(loggedInUser.Id))
            {
                return Forbid();
            }

            await _userService.ChangeUsersRoleAsync(bulkChangeUsersRoleDto);

            return Ok();
        }

        [HttpGet("profile-picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfilePicture()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            DownloadInfoDto? response = await _azureBlobService.DownloadProfilePictureAsync(loggedInUser.Id);

            return response != null
                ? File(await response.Content, response.ContentType)
                : NotFound();
        }

        [HttpPut("[action]")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ProfilePicture([FromForm][Required] IFormFile file)
        {
            await using MemoryStream? memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            UserDto? loggedInUser = GetLoggedInUser();

            await _userService.ChangeProfilePictureAsync(loggedInUser.Id, memoryStream);

            return Ok();
        }

        [HttpDelete("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ProfilePicture()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _userService.DeleteProfilePictureAsync(loggedInUser.Id);

            return Ok();
        }

        /// <returns>A boolean indicating if the user will need to confirm the new email address.</returns>
        [HttpPut("[action]")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Settings([FromBody] UserSettingsDto dto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            string? baseUrl = $"{Request.Scheme}://{Request.Host.Value}/";

            bool result = await _userService.ChangeSettingsAsync(loggedInUser.Id, dto);

            return Ok(result);
        }

        [HttpPut("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            Microsoft.AspNetCore.Identity.IdentityResult? result = await _userService.ChangePasswordAsync(loggedInUser.Id, dto);

            return result.Succeeded
                ? Ok()
                : BadRequest(result.Errors);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Invite([FromBody] UserInvitationDto inviteUserDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();
            string? baseUrl = $"{Request.Scheme}://{Request.Host.Value}/";

            try
            {
                await _userService.InviteUserAsync(inviteUserDto, loggedInUser);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterInvitation([FromBody] RegisterInvitationDto registerInvitationDto)
        {
            Response<RegisterInvitationDto> registrationResponse;

            registrationResponse = await _userService.RegisterInvitationAsync(registerInvitationDto);

            if (registrationResponse.Dto != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, registrationResponse.ErrorMessage);
            }

            return registrationResponse.IsSuccessful ? Ok() : BadRequest(registrationResponse.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPut("is-user-already-registered")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsInvitedUserAlreadyRegistered([FromBody] InvitedUserDto registerInvitationDto)
        {
            return Ok(await _userService.IsInvitedUserAlreadyRegistered(registerInvitationDto));
        }

        [HttpPut("[action]/{id}")]
        [Authorize(Roles = UserRole.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleTwoFactorAuthentication(string id)
        {
            await _userService.ToggleTwoFactorAuthenticationAsync(id);
            return Ok();
        }

        [HttpPut("[action]/{id}")]
        [Authorize(Roles = UserRole.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MakeUserSuperAdmin(string id)
        {
            await _userService.MakeUserSuperAdminAsync(id);
            return Ok();
        }

        [HttpPut("[action]/{id}")]
        [Authorize(Roles = UserRole.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveUserSuperAdmin(string id)
        {
            await _userService.RemoveUserSuperAdminAsync(id);
            return Ok();
        }

    }
}