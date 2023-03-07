using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GraphConfigController : HorizonBaseController
    {
        private readonly IGraphConfigService _graphConfigService;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly ILogger _logger;

        public GraphConfigController(IGraphConfigService graphConfigService, IDeployIntunewinService deployIntunewinService,
            ILogger<GraphConfigController> logger)
        {
            _graphConfigService = graphConfigService;
            _deployIntunewinService = deployIntunewinService;
            _logger = logger;
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateGraphConfig([FromBody] GraphConfigDto graphConfigDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _graphConfigService.CreateGraphConfigAsync(graphConfigDto, loggedInUser.SubscriptionId);

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(loggedInUser.SubscriptionId);

                // update device count only if CheckGraphStatus doesn't throw
                await _deployIntunewinService.UpdateDevicesCountAsync();
            }
            catch
            {
                _logger.LogWarning("Invalid Graph Config details, trust couldn't be established; device count not updated.");
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpGet]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckGraphConfig()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _graphConfigService.HasGraphConfigAsync(loggedInUser.SubscriptionId);

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckGraphConfigStatus()
        {
            string errorMessage = string.Empty;
            UserDto? loggedInUser = GetLoggedInUser();

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(loggedInUser.SubscriptionId);
            }
            catch (MsalServiceException msalServiceException)
            {
                errorMessage = msalServiceException.ErrorCode switch
                {
                    MsalError.InvalidClient => "Invalid authorization information. Please check if secret value is valid.",
                    MsalError.InvalidRequest => "Invalid authorization information. Please check if tenant value is valid.",
                    "unauthorized_client" => "Invalid authorization information. Invalid client or client Id",
                    "missing_claims" => msalServiceException.Message,
                    _ => "Invalid authorization information."
                };
            }
            catch
            {
                return StatusCode(500, "Cannot establish trust with Microsoft Intune.");
            }

            return !string.IsNullOrEmpty(errorMessage) ? BadRequest(errorMessage) : Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveGraphConfig()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            try
            {
                await _graphConfigService.RemoveGraphConfigAsync(loggedInUser.SubscriptionId);
            }
            catch
            {
                return StatusCode(500, "Cannot remove graph configuration");
            }

            return Ok();
        }
    }
}
