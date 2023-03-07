using Microsoft.AspNetCore.Http;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IPublicApplicationService
    {
        /// <summary>
        /// Lists all public applications and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific public application by name</param>
        /// <returns>A paged result with all public applications paged</returns>
        Task<PagedResult<PublicApplicationDto>> ListPublicApplicationsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm);

        /// <summary>
        /// Creates a list with the ids of public applications
        /// </summary>
        /// <returns>An enumerable containing the ids of the public applications</returns>
        Task<IEnumerable<int>> ListPublicApplicationsIdsAsync();

        /// <summary>
        /// Handles the action of adding or updating a public application
        /// </summary>
        /// <param name="publicApplicationDto">The public application information that is about to be added or changed</param>
        /// <returns>The id of the application that was added or changed</returns>
        Task<int> AddOrUpdatePublicApplicationAsync(PublicApplicationDto publicApplicationDto);

        /// <summary>
        /// Handles the action of removing a public application
        /// </summary>
        /// <param name="applicationIds">The ids of the public applications we want to remove</param>
        /// <returns>Void</returns>
        Task RemovePublicApplicationsAsync(int[] applicationIds);

        /// <summary>
        /// Gets the download link for an application from public repository
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get the download link</param>
        /// <returns>The download link</returns>
        Task<Uri?> GetDownloadUriForPublicApplicationAsync(int applicationId);

        /// <summary>
        /// Handles the auto-update action of a public application
        /// </summary>
        /// <param name="applicationId">The id of the application that has the auto-update option enabled</param>
        /// <param name="autoUpdate">A bool that determines if the auto-update option is enabled or not</param>
        /// <returns>A bool indicating if the public application has the auto-update option enabled or not</returns>
        Task<bool> UpdateSubscriptionPublicApplicationAutoUpdateAsync(int applicationId, bool autoUpdate);

        /// <summary>
        /// Handles the manual-approve action of a public application
        /// </summary>
        /// <param name="applicationId">The id of the application that has the manual-approve option enabled</param>
        /// <param name="manualApprove">A bool that determines if the manual-approve option is enabled or not</param>
        /// <returns>A bool indicating if the public application has the manual-approve option enabled or not</returns>
        Task<bool> UpdateSubscriptionPublicApplicationManualApproveAsync(int applicationId, bool manualApprove);

        /// <summary>
        /// Starts the deployment of the public applications
        /// </summary>
        /// <param name="applicationIds">The ids of the private applications we want to deploy</param>
        /// <returns>A status code representing the state of the action</returns>
        Task<Result> StartDeployAsync(IEnumerable<int> applicationIds);

        /// <summary>
        /// Uploads an application
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<Response<ApplicationDto>> UploadAsync(IFormFile file);

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="applicationId"></param>
        ///// <returns></returns>
        //Task<int[]> CopyApplicationsAsync(int[] applicationIds);

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="loggedInUser"></param>
        ///// <param name="applicationIds"></param>
        ///// <returns></returns>
        //Task<int[]> CopyApplicationsJobAsync(Guid subscriptionId, int[] applicationIds);
    }
}
