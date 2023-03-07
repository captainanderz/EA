using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.WebAPI.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : HorizonBaseController
    {
        private readonly ILogger<AuthController> log;

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            log = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(Response<FarPayResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegistrationDto registrationDto)
        {
            string? baseUrl = $"{Request.Scheme}://{Request.Host.Value}/";

            Response<FarPayResult> result = await _authService.CreateAccountAndSubscriptionAsync(registrationDto);

            return result.IsSuccessful ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(Response<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            log.LogDebug("{email} logging in", loginDto.Email);
            Response<UserDto>? response = await _authService.LoginAsync(loginDto);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return Ok(response);
        }

        [AllowEnterMfaCode]
        [HttpPost]
        [ProducesResponseType(typeof(Response<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Response<UserDto>>> LoginMfaCode([FromBody] MfaCodeDto dto)
        {
            UserDto? user = GetLoggedInUser();

            Response<UserDto>? response = await _authService.LoginMfaCodeAsync(user.Id, dto.Code);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return response;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Response<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> NewTokens()
        {
            string? refreshToken = Request.Cookies[AuthConstants.RefreshTokenCookieName];

            Response<UserDto>? response = await _authService.GetNewTokensAsync(refreshToken);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RevokeRefreshToken()
        {
            string? refreshToken = Request.Cookies[AuthConstants.RefreshTokenCookieName];
            UserDto? user = GetLoggedInUser();

            await _authService.RevokeRefreshTokenAsync(user.Id, refreshToken);

            return Ok();
        }

        [AllowEnterMfaCode]
        [HttpPost]
        [ProducesResponseType(typeof(Response<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Response<UserDto>>> LoginRecoveryCode([FromBody] string code)
        {
            UserDto? user = GetLoggedInUser();

            Response<UserDto>? response = await _authService.LoginRecoveryCode(user.Id, code);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return response;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationDto emailConfirmationDto)
        {
            bool confirmSuccessful = await _authService.ConfirmEmailAsync(emailConfirmationDto);

            return confirmSuccessful ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmChangeEmail([FromBody] EmailConfirmationDto emailConfirmationDto)
        {
            bool confirmSuccessful = await _authService.ConfirmChangeEmailAsync(emailConfirmationDto);

            return confirmSuccessful ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody][Required][EmailAddress] string email)
        {
            string? baseUrl = $"{Request.Scheme}://{Request.Host.Value}/";

            await _authService.SendPasswordResetEmailAsync(email);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto passwordResetDto)
        {
            bool resetSuccessful = await _authService.ResetPasswordAsync(passwordResetDto);

            return resetSuccessful ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [AllowConfigureMfa]
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> AuthenticatorKey()
        {
            UserDto? user = GetLoggedInUser();

            return await _authService.GenerateAuthenticatorKeyAsync(user.Id);
        }

        [AllowConfigureMfa]
        [HttpPost]
        [ProducesResponseType(typeof(Response<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<Response<UserDto>>> EnableTwoFactor([FromBody] MfaCodeDto dto)
        {
            UserDto? user = GetLoggedInUser();

            Response<UserDto>? response = await _authService.EnableTwoFactorAsync(user.Id, dto.Code);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return response;
        }

        [AllowConfigureMfa]
        [HttpGet]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> RecoveryCodesNumber()
        {
            UserDto? user = GetLoggedInUser();

            return await _authService.GetRecoveryCodesNumberAsync(user.Id);
        }

        [AllowConfigureMfa]
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<string>>> GenerateNewTwoFactorRecoveryCodes()
        {
            UserDto? user = GetLoggedInUser();

            IEnumerable<string>? result = await _authService.GenerateNewTwoFactorRecoveryCodesAsync(user.Id);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            return await _authService.CheckIfEmailIsDuplicateAsync(email);
        }
    }
}