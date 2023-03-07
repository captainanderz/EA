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
    public class AzureGroupsController : HorizonBaseController
    {
        private readonly IAzureGroupService _azureGroupService;

        public AzureGroupsController(IAzureGroupService azureGroupService)
        {
            _azureGroupService = azureGroupService;
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<AzureGroupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromQuery] string? groupName, [FromQuery] string? nextPageLink)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            var result = await _azureGroupService.FilterAzureGroupsByNameAsync(loggedInUser.SubscriptionId, groupName, nextPageLink);

            return Ok(result);
        }
    }
}