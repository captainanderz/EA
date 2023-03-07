using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IDeploymentScheduleService
    {
        /// <summary>
        /// Handles the action of paginating the assignment profiles.
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns></returns>
        Task<PagedResult<DeploymentScheduleDto>> ListPagedAsync(int pageNumber, int pageSize, string searchTerm);

        /// <summary>
        /// Handles the action of getting a list with all the assignment profile ids
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<long>> ListIdsAsync();

        /// <summary>
        /// Handles the action of returning an assignment profile by a given id.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task<DeploymentScheduleDetailsDto> GetAsync(long id);

        /// <summary>
        /// Handles the action of filtering of all assignment profiles, by a given name.
        /// </summary>
        /// <param name="name">The name of the assignment profile we want to filter by</param>
        /// <returns></returns>
        Task<IEnumerable<DeploymentScheduleDto>> FilterByNameAsync(string? name);

        /// <summary>
        /// Handles the action of adding a new assignment profile into the database.
        /// </summary>
        /// <param name="assignmentProfileDto">The new assignment profile dto</param>
        /// <returns></returns>
        Task<long> AddAsync(DeploymentScheduleDetailsDto dto);

        /// <summary>
        /// Handles the action of removing an assignment profile from the database.
        /// </summary>
        /// <param name="assignmentId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        Task<int> RemoveAsync(DeploymentScheduleRemoveDto dto);

        /// <summary>
        /// Handles the action of copying an assignment profile and adding it into the database.
        /// </summary>
        /// <param name="assignmentProfileIds">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        Task<int> CopyAsync(long[] ids);

        /// <summary>
        /// Handles the action of editing an assignment profile.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="newAssignmentProfileDto">The assignment profile dto to be edited</param>
        /// <returns></returns>
        Task<Result> EditAsync(long id, DeploymentScheduleDetailsDto dto);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a private application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignToPrivateApplicationsAsync(long id, DeploymentScheduleAssignmentDto dto);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a public application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignToPublicApplicationsAsync(long id, DeploymentScheduleAssignmentDto dto);


        /// <summary>
        /// Handles the action of clearing the assignment profile from the private applications.
        /// </summary>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Task<int> ClearFromPrivateApplicationsAsync(DeploymentScheduleClearDto dto);

        /// <summary>
        /// Handles the action of clearing the assignment profiles from public applications.
        /// </summary>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> ClearFromPublicApplicationsAsync(DeploymentScheduleClearDto dto);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task DeletePrivateApplicationPatchAppsAsync(int[] ids);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task DeletePublicApplicationPatchAppsAsync(int[] ids);
    }
}
