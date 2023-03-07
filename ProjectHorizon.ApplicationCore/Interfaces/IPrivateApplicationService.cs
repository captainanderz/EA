using Microsoft.AspNetCore.Http;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IPrivateApplicationService
    {
        /// <summary>
        /// Lists all private applications and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which than pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns>A paged result with all private applications paged</returns>
        Task<PagedResult<PrivateApplicationDto>> ListPrivateApplicationsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm);

        /// <summary>
        /// Creates a list with the ids of private applications
        /// </summary>
        /// <returns>An enumerable containing the ids of the private applications</returns>
        Task<IEnumerable<int>> ListPrivateApplicationsIdsAsync();

        /// <summary>
        /// Handles the action of removing a private application
        /// </summary>
        /// <param name="applicationIds">The ids of the private applications we want to remove</param>
        /// <returns>A status representing the state of the action</returns>
        Task<int> RemovePrivateApplicationsAsync(int[] applicationIds);

        /// <summary>
        /// Handles the action of adding or updating a private application
        /// </summary>
        /// <param name="privateApplicationDto">The private application information that is about to be added or changed</param>
        /// <returns>The id of the application that was added or changed</returns>
        Task<int> AddOrUpdatePrivateApplicationAsync(PrivateApplicationDto publicApplicationDto);

        /// <summary>
        /// Gets the download link for an application from private repository
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get the download link</param>
        /// <returns>The key value pair with the download link and a status code</returns>
        Task<(Uri, int)> GetDownloadUriForPrivateApplicationAsync(int applicationId);

        /// <summary>
        /// Starts the deployment of a private application
        /// </summary>
        /// <param name="applicationIds">The id of the private application we want to deploy</param>
        /// <returns>A status code representing the state of the action</returns>
        Task<Result> StartDeployAsync(IEnumerable<int> applicationIds);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<Response<ApplicationDto>> UploadAsync(IFormFile file);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<int[]> CopyApplicationsAsync(int[] applicationIds);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggedInUser"></param>
        /// <param name="applicationIds"></param>
        /// <returns></returns>
        Task<int[]> CopyApplicationsJobAsync(UserDto loggedInUser, int[] applicationIds);
    }
}
