using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Services.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        public ApprovalService(
            IApplicationDbContext applicationDbContext,
            IDeployIntunewinService deployIntunewinService,
            IHubContext<SignalRHub> hubContext,
            IBackgroundJobService backgroundJobService,
            IAuditLogService auditLogService, 
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationDbContext = applicationDbContext;
            _deployIntunewinService = deployIntunewinService;
            _hubContext = hubContext;
            _backgroundJobService = backgroundJobService;
            _auditLogService = auditLogService;
            _loggedInUserProvider = loggedInUserProvider;
        }

        /// <summary>
        /// Lists all approvals and paginate them
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <returns></returns>
        public async Task<PagedResult<ApprovalDto>> ListApprovalsPagedAsync(int pageNumber, int pageSize)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            IQueryable<Approval>? queryApprovals = _applicationDbContext.Approvals.Where(a => a.SubscriptionId == subscriptionId && a.IsActive);

            return new PagedResult<ApprovalDto>
            {
                AllItemsCount = await queryApprovals.CountAsync(),
                PageItems = await queryApprovals
                .Join(
                    _applicationDbContext.SubscriptionPublicApplications,
                    a => new { a.PublicApplicationId, a.SubscriptionId },
                    spa => new { spa.PublicApplicationId, spa.SubscriptionId },
                    (a, spa) => new
                    {
                        Approval = a,
                        SubscriptionPublicApplication = spa,
                    }
                )
               .OrderByDescending(group => group.Approval.CreatedOn)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .Select(group => new ApprovalDto
               {
                   Id = group.Approval.Id,
                   CreatedOn = group.Approval.CreatedOn,
                   ModifiedOn = group.Approval.ModifiedOn,
                   NewVersion = group.Approval.PublicApplication.Version,
                   Architecture = group.Approval.PublicApplication.Architecture,
                   Name = group.Approval.PublicApplication.Name,
                   IconBase64 = group.Approval.PublicApplication.IconBase64,
                   DeployedVersion = group.SubscriptionPublicApplication.DeployedVersion
               })
               .ToListAsync()
            };
        }

        /// <summary>
        /// Updates the state of the approval
        /// </summary>
        /// <param name="approvalIds">The id of the approval we want to update</param>
        /// <param name="approvalDecision">The decision the user made for the respective approval</param>
        /// <returns></returns>
        public async Task<int> UpdateApprovalStatusAsync(IEnumerable<int> approvalIds, string approvalDecision)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;

            ICollection<Approval> approvals = await _applicationDbContext
                .Approvals
                .Where(ap => approvalIds.Contains(ap.Id))
                .Include(ap => ap.PublicApplication)
                .ToListAsync();

            if (approvals.Any(a => a.SubscriptionId != subscriptionId))
            {
                return StatusCodes.Status401Unauthorized;
            }

            foreach (Approval? item in approvals)
            {
                item.IsActive = false;
                item.UserDecision = approvalDecision;

                if (approvalDecision == ApprovalDecision.Approved)
                {
                    var subPubApp = item.SubscriptionPublicApplication;

                    if (subPubApp.DeploymentSchedule is null)
                    {
                        _backgroundJobService.Enqueue(() => _deployIntunewinService.DeployPublicApplicationForSubscriptionAsync(loggedInUser, item.SubscriptionId, item.PublicApplicationId, null, false));
                    }
                }

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.Approvals,
                    string.Format(AuditLogActions.Approvals, item.UserDecision.ToString().ToLower(), item.PublicApplication.Name),
                    author: loggedInUser,
                    saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            var subscriptionUserIds = await _applicationDbContext.SubscriptionUsers
                    .Where(subscriptionUser => subscriptionUser.SubscriptionId == loggedInUser.SubscriptionId)
                    .Select(subscriptionUser => subscriptionUser.ApplicationUserId)
                    .ToArrayAsync();

            await _hubContext.Clients.Users(subscriptionUserIds).SendAsync(SignalRMessages.UpdateApprovalCount);

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Gets all the approvals of the current subscription and counts them
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetApprovalsCountAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            return await _applicationDbContext
                .Approvals
                .Where(ap => ap.IsActive && ap.SubscriptionId == subscriptionId)
                .CountAsync();
        }

        /// <summary>
        /// Lists all approvals ids
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<int>> ListApprovalsIdsAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            return await _applicationDbContext
                .Approvals
                .Where(ap => ap.IsActive && ap.SubscriptionId == subscriptionId)
                .Select(s => s.Id)
                .ToListAsync();
        }
    }
}