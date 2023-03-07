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
    public class ApprovalsController : HorizonBaseController
    {
        private readonly IApprovalService _approvalService;

        public ApprovalsController(IApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ApprovalDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListApprovals([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            PagedResult<ApprovalDto>? result = await _approvalService.ListApprovalsPagedAsync(pageNumber, pageSize);

            return Ok(result);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<ApprovalDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Count()
        {
            int approvalCount = await _approvalService.GetApprovalsCountAsync();

            return Ok(approvalCount);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ids()
        {
            IEnumerable<int>? result = await _approvalService.ListApprovalsIdsAsync();

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPut("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Approve([FromBody] IEnumerable<int> approvalIds)
        {
            int statusCode =
                await _approvalService.UpdateApprovalStatusAsync(approvalIds, ApprovalDecision.Approved);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPut("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject([FromBody] IEnumerable<int> approvalIds)
        {
            int statusCode =
                await _approvalService.UpdateApprovalStatusAsync(approvalIds, ApprovalDecision.Rejected);

            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return Unauthorized();
            }

            return Ok();
        }
    }
}
