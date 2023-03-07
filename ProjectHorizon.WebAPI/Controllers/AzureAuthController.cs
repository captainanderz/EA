using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Policy = AuthConstants.AzurePolicy)]
    public class AzureAuthController : HorizonBaseController
    {
        // The Web API will only accept tokens 1) for users, and 
        // 2) having the access_as_user scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "access_as_user" };

        private readonly IAuthService _authService;

        public AzureAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegistrationDto registrationDto)
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            registrationDto.Email = User.Identity.Name;

            Response<string>? result = await _authService.CreateAzureAccountAsync(registrationDto);

            if (!result.IsSuccessful && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            string email = User.Identity.Name;

            Response<UserDto> response = await _authService.LoginAzureAccountAsync(email);

            if (response.IsSuccessful)
            {
                SetRefreshTokenCookie(response.Dto.RefreshToken);
            }

            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SimpleLogin()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            ClaimsIdentity? userClaims = User.Identity as ClaimsIdentity;
            string? tenantId = userClaims?.FindFirst(ClaimConstants.TenantId)?.Value;
            string? userId = userClaims?.FindFirst(ClaimConstants.ObjectId)?.Value;
            string? name = userClaims?.FindFirst(ClaimConstants.Name)?.Value;
            string? email = userClaims.FindFirst(ClaimTypes.Upn)?.Value;

            if (tenantId is null || userId is null || email is null)
            {
                return Unauthorized();
            }

            SimpleLoginDto? loginDto = new SimpleLoginDto
            {
                Id = userId,
                TenantId = tenantId,
                Name = name ?? email,
                Email = email,
            };

            Response<SimpleUserDto>? user = await _authService.LoginSimpleAzureAccountAsync(loginDto);

            return Ok(user);
        }
    }
}