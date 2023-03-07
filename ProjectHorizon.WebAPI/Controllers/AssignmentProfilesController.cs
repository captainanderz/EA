using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentProfilesController : HorizonBaseController
    {
        private readonly IAssignmentProfileService _assignmentProfileService;

        public AssignmentProfilesController(IAssignmentProfileService assignmentProfileService)
        {
            _assignmentProfileService = assignmentProfileService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AssignmentProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? searchTerm)
        {
            PagedResult<AssignmentProfileDto>? result = await _assignmentProfileService.ListAssignmentProfilesPagedAsync(pageNumber, pageSize, searchTerm);

            return Ok(result);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ids()
        {
            IEnumerable<long>? result = await _assignmentProfileService.ListAssignmentProfileIdsAsync();

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add([FromBody] NewAssignmentProfileDto newAssignmentProfileDto)
        {
            long assignmentProfileId = await _assignmentProfileService
                .AddAssignmentProfileAsync(newAssignmentProfileDto);

            return Ok(assignmentProfileId);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Remove([FromBody] long[] assignmentProfileIds)
        {
            int statusCode =
                await _assignmentProfileService.RemoveAssignmentProfileAsync(assignmentProfileIds);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromQuery] string? assignmentProfileName)
        {
            IEnumerable<AssignmentProfileDto> assignmentProfiles = new List<AssignmentProfileDto>();

            assignmentProfiles = await _assignmentProfileService.FilterAssignmentProfilesByNameAsync(assignmentProfileName);

            return Ok(assignmentProfiles);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{assignmentProfileId:long}/[action]")]
        public async Task<IActionResult> AssignPrivateApplications([FromRoute] long assignmentProfileId, [FromBody] int[] applicationIds)
        {
            int statusCode = await _assignmentProfileService.AssignAssignmentProfileToPrivateApplicationsAsync(assignmentProfileId, applicationIds);

            if (statusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{assignmentProfileId:long}/[action]")]
        public async Task<IActionResult> AssignPublicApplications([FromRoute] long assignmentProfileId, [FromBody] int[] applicationIds)
        {
            int statusCode = await _assignmentProfileService.AssignAssignmentProfileToPublicApplicationsAsync(assignmentProfileId, applicationIds);

            if (statusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> Copy([FromBody] long[] assignmentProfileIds)
        {
            int result = await _assignmentProfileService.CopyAssignmentProfileAsync(assignmentProfileIds);

            return StatusCode(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("{assignmentProfileId:long}")]
        public async Task<IActionResult> GetById([FromRoute] long assignmentProfileId)
        {
            AssignmentProfileDetailsDto? assignmentProfileDetails = await _assignmentProfileService.GetAssignmentProfileAsync(assignmentProfileId);

            return Ok(assignmentProfileDetails);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("{assignmentProfileId:long}/[action]")]
        public async Task<IActionResult> Edit([FromRoute] long assignmentProfileId, [FromBody] NewAssignmentProfileDto editedAssignmentProfileDto)
        {
            long assignmentProfileToEditId = await _assignmentProfileService.EditAssignmentProfileAsync(assignmentProfileId, editedAssignmentProfileDto);

            return Ok(assignmentProfileToEditId);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> ClearPrivateApplications([FromBody] int[] applicationIds)
        {
            long assignmentProfileToClearId = await _assignmentProfileService.ClearAssignedProfilesFromPrivateApplicationsAsync(applicationIds);

            return Ok(assignmentProfileToClearId);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("[action]")]
        public async Task<IActionResult> ClearPublicApplications([FromBody] int[] applicationIds)
        {
            long assignmentProfileToClearId = await _assignmentProfileService.ClearAssignedProfilesFromPublicApplicationsAsync(applicationIds);

            return Ok(assignmentProfileToClearId);
        }
    }
}
