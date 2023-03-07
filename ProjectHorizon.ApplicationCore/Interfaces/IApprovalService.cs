using ProjectHorizon.ApplicationCore.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IApprovalService
    {
        /// <summary>
        /// Lists all approvals and paginate them
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="pageNumber">The number from which tha pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <returns></returns>
        Task<PagedResult<ApprovalDto>> ListApprovalsPagedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Updates the state of the approval
        /// </summary>
        /// <param name="approvalIds">The id of the approval we want to update</param>
        /// <param name="approvalDecision">The decision the user made for the respective approval</param>
        /// <param name="userSubscriptionId">The id of the current subscription</param>
        /// <returns></returns>
        Task<int> UpdateApprovalStatusAsync(IEnumerable<int> approvalIds, string approvalDecision);

        /// <summary>
        /// Gets all the approvals of the current subscription and counts them
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns></returns>
        Task<int> GetApprovalsCountAsync();

        /// <summary>
        /// Lists all approvals ids
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns></returns>
        Task<IEnumerable<int>> ListApprovalsIdsAsync();
    }
}
