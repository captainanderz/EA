using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuditLogsController : HorizonBaseController
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Paged(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string category)
        {
            if (!AuditLogCategory.Values.Contains(category))
            {
                return BadRequest();
            }

            PagedResult<AuditLogDto>? result = await _auditLogService
                .ListAuditLogsPagedAsync(pageNumber, pageSize, searchTerm, fromDate, toDate, category);

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Csv(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? searchTerm,
            [FromQuery] string category)
        {
            if (!AuditLogCategory.Values.Contains(category))
            {
                return BadRequest();
            }

            string? result = await _auditLogService.GetCsvAsync(searchTerm, fromDate, toDate, category);

            return File(Encoding.UTF8.GetBytes(result), "text/csv", "audit-log.csv");
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminConsent()
        {
            var loggedInUser = GetLoggedInUser();
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.SingleSignOn, 
                "Admin consent granted for Azure AD",
                author: loggedInUser);

            return Ok();
        }
    }
}