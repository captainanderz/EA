using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class PrivateApplicationsController : HorizonBaseController
    {
        private const long requestSizeLimit = 8589934592; // 8 Gigabytes

        private readonly IPrivateApplicationService _privateApplicationService;

        public PrivateApplicationsController(
            IPrivateApplicationService privateApplicationService)
        {
            _privateApplicationService = privateApplicationService;
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(requestSizeLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
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
                Response<ApplicationDto> response = await _privateApplicationService.UploadAsync(file);

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

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPut("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddOrUpdate(PrivateApplicationDto privateApplicationDto)
        {
            int applicationId = await _privateApplicationService
                .AddOrUpdatePrivateApplicationAsync(privateApplicationDto);

            return Ok(applicationId);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PrivateApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListPrivateApplicationsPaged(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm)
        {
            PagedResult<PrivateApplicationDto>? result = await _privateApplicationService
                .ListPrivateApplicationsPagedAsync(pageNumber, pageSize, searchTerm);

            return Ok(result);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ids()
        {
            IEnumerable<int>? result = await _privateApplicationService.ListPrivateApplicationsIdsAsync();

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        public async Task<IActionResult> Deploy([FromBody] IEnumerable<int> applicationIds)
        {
            return await _privateApplicationService.StartDeployAsync(applicationIds) switch
            {
                SuccessResult => Ok(),
                NotFoundResult result => NotFound(result.Message),
                InvalidOperationResult result => BadRequest(result.Message),
                _ => throw new NotImplementedException(),
            };
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Remove([FromBody] int[] applicationIds)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            int statusCode =
                await _privateApplicationService.RemovePrivateApplicationsAsync(applicationIds);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok();
        }

        [HttpGet("{applicationId:int}/[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DownloadUri(int applicationId)
        {
            (Uri downloadUri, int statusCode) = await _privateApplicationService
                .GetDownloadUriForPrivateApplicationAsync(applicationId);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok(downloadUri.AbsoluteUri);
        }
    }
}