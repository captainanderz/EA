using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Results;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NotFoundResult = ProjectHorizon.ApplicationCore.Results.NotFoundResult;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicApplicationsController : HorizonBaseController
    {
        private const long requestSizeLimit = 8589934592; // 8 Gigabytes

        private readonly IPublicApplicationService _publicApplicationsService;
        private readonly IDeployIntunewinService _deployIntunewinService;

        public PublicApplicationsController(
            IPublicApplicationService publicApplicationsService,
            IDeployIntunewinService deployIntunewinService)
        {
            _publicApplicationsService = publicApplicationsService;
            _deployIntunewinService = deployIntunewinService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PublicApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListPublicApplicationsPaged(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm)
        {
            PagedResult<PublicApplicationDto>? result = await _publicApplicationsService.ListPublicApplicationsPagedAsync(
                pageNumber,
                pageSize,
                searchTerm);

            return Ok(result);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ids()
        {
            IEnumerable<int>? result = await _publicApplicationsService.ListPublicApplicationsIdsAsync();

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin)]
        [HttpPost("[action]")]
        [RequestSizeLimit(requestSizeLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload()
        {
            if (Request.Form.Files.Count != 1)
            {
                return BadRequest("Multiple file upload not supported");
            }

            IFormFile file = Request.Form.Files[0];

            if (file.Length <= 0)
            {
                return BadRequest($"Invalid file received");
            }

            try
            {
                Response<ApplicationDto> response = await _publicApplicationsService.UploadAsync(file);

                if (!response.IsSuccessful)
                {
                    return BadRequest(response.ErrorMessage);
                }

                return Ok(response.Dto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex}");
            }
        }

        [Authorize(Roles = UserRole.SuperAdmin)]
        [HttpPut("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddOrUpdate(PublicApplicationDto publicApplicationDto)
        {
            int applicationId = await _publicApplicationsService.AddOrUpdatePublicApplicationAsync(publicApplicationDto);

            return Ok(applicationId);
        }

        [Authorize(Roles = UserRole.SuperAdmin)]
        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Remove([FromBody] int[] applicationIds)
        {
            await _publicApplicationsService.RemovePublicApplicationsAsync(applicationIds);

            return Ok();
        }

        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        public async Task<IActionResult> Deploy([FromBody] IEnumerable<int> applicationIds)
        {
            return await _publicApplicationsService.StartDeployAsync(applicationIds) switch
            {
                SuccessResult => Ok(),
                NotFoundResult result => NotFound(result.Message),
                InvalidOperationResult result => BadRequest(result.Message),
                _ => throw new NotImplementedException(),
            };
        }

        [HttpDelete("{applicationId:int}/[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        public async Task<IActionResult> RemoveDeploy(int applicationId)
        {
            await _deployIntunewinService.RemoveDeployedPublicApplicationAsync(applicationId);

            return Ok();
        }

        [HttpGet("{applicationId:int}/deployment")]
        [ProducesResponseType(typeof(DeploymentInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeploymentInfo(int applicationId)
        {
            UserDto? loggedInUser = GetLoggedInUser();
            MobileLobApp deployedApplication =
                await _deployIntunewinService.GetDeployedPublicApplicationInfoAsync(applicationId);

            DeploymentInfoDto deploymentInfo = new DeploymentInfoDto();
            if (deployedApplication is Win32LobApp win32)
            {
                deploymentInfo.Name = win32.DisplayName;
                deploymentInfo.Version = win32.DisplayVersion;
            }
            else
            {
                return StatusCode(404, "Deployed application not found");
            }

            return Ok(deploymentInfo);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPatch("{applicationId:int}/[action]/{autoUpdate:bool}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> AutoUpdate(int applicationId, bool autoUpdate)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _publicApplicationsService
                .UpdateSubscriptionPublicApplicationAutoUpdateAsync(applicationId, autoUpdate);

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPatch("{applicationId:int}/[action]/{manualApprove:bool}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManualApprove(int applicationId, bool manualApprove)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _publicApplicationsService
                .UpdateSubscriptionPublicApplicationManualApproveAsync(applicationId, manualApprove);

            return Ok(result);
        }

        [HttpGet("{applicationId:int}/[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadUri(int applicationId)
        {
            Uri? downloadUri = await _publicApplicationsService.GetDownloadUriForPublicApplicationAsync(applicationId);

            return downloadUri switch
            {
                Uri uri => Ok(uri.AbsoluteUri),
                null => Forbid()
            };
        }
    }
}