using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Results;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotFoundResult = ProjectHorizon.ApplicationCore.Results.NotFoundResult;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeploymentSchedulesController : HorizonBaseController
    {
        private readonly IDeploymentScheduleService _deploymentScheduleService;

        public DeploymentSchedulesController(IDeploymentScheduleService deploymentScheduleService)
        {
            _deploymentScheduleService = deploymentScheduleService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DeploymentScheduleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? searchTerm)
        {
            PagedResult<DeploymentScheduleDto>? result = await _deploymentScheduleService.ListPagedAsync(pageNumber, pageSize, searchTerm);

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromQuery] string? deploymentScheduleName)
        {
            IEnumerable<DeploymentScheduleDto> deploymentSchedules = new List<DeploymentScheduleDto>();

            deploymentSchedules = await _deploymentScheduleService.FilterByNameAsync(deploymentScheduleName);

            return Ok(deploymentSchedules);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ids()
        {
            IEnumerable<long>? result = await _deploymentScheduleService.ListIdsAsync();

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add([FromBody] DeploymentScheduleDetailsDto newDto)
        {
            long id = await _deploymentScheduleService
                .AddAsync(newDto);

            return Ok(id);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Remove([FromBody] DeploymentScheduleRemoveDto dto)
        {
            int statusCode =
                await _deploymentScheduleService.RemoveAsync(dto);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok();
        }


        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> Copy([FromBody] long[] ids)
        {
            int result = await _deploymentScheduleService.CopyAsync(ids);

            return StatusCode(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById([FromRoute] long id)
        {
            DeploymentScheduleDetailsDto? details = await _deploymentScheduleService.GetAsync(id);

            return Ok(details);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{id:long}/[action]")]
        public async Task<IActionResult> Edit([FromRoute] long id, [FromBody] DeploymentScheduleDetailsDto newDto)
        {
            return await _deploymentScheduleService.EditAsync(id, newDto) switch
            {
                SuccessResult => Ok(),
                NotFoundResult result => NotFound(result.Message),
                InvalidOperationResult result => BadRequest(result.Message),
                _ => throw new System.NotImplementedException(),
            };
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{id:long}/[action]")]
        public async Task<IActionResult> AssignPrivateApplications([FromRoute] long id, [FromBody] DeploymentScheduleAssignmentDto dto)
        {
            int statusCode = await _deploymentScheduleService.AssignToPrivateApplicationsAsync(id, dto);

            if (statusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{id:long}/[action]")]
        public async Task<IActionResult> AssignPublicApplications([FromRoute] long id, [FromBody] DeploymentScheduleAssignmentDto dto)
        {
            int statusCode = await _deploymentScheduleService.AssignToPublicApplicationsAsync(id, dto);

            if (statusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> ClearPrivateApplications([FromBody] DeploymentScheduleClearDto deploymentScheduleClearDto)
        {
            long id = await _deploymentScheduleService.ClearFromPrivateApplicationsAsync(deploymentScheduleClearDto);

            return Ok(id);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> ClearPublicApplications([FromBody] DeploymentScheduleClearDto deploymentScheduleClearDto)
        {
            long id = await _deploymentScheduleService.ClearFromPublicApplicationsAsync(deploymentScheduleClearDto);

            return Ok(id);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeletePrivateApplicationPatchApps([FromBody] int[] applicationIds)
        {
            await _deploymentScheduleService.DeletePrivateApplicationPatchAppsAsync(applicationIds);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeletePublicApplicationPatchApps([FromBody] int[] applicationIds)
        {
            await _deploymentScheduleService.DeletePublicApplicationPatchAppsAsync(applicationIds);

            return Ok();
        }
    }
}
