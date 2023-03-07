using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAssignmentProfileService
    {
        /// <summary>
        /// Handles the action of paginating the assignment profiles.
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns></returns>
        Task<PagedResult<AssignmentProfileDto>> ListAssignmentProfilesPagedAsync(int pageNumber, int pageSize, string searchTerm);

        /// <summary>
        /// Handles the action of getting a list with all the assignment profile ids
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<long>> ListAssignmentProfileIdsAsync();

        /// <summary>
        /// Handles the action of returning an assignment profile by a given id.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task<AssignmentProfileDetailsDto> GetAssignmentProfileAsync(long assignmentProfileId);

        /// <summary>
        /// Handles the action of filtering of all assignment profiles, by a given name.
        /// </summary>
        /// <param name="assignmnetProfileName">The name of the assignment profile we want to filter by</param>
        /// <returns></returns>
        Task<IEnumerable<AssignmentProfileDto>> FilterAssignmentProfilesByNameAsync(string assignmnetProfileName);

        /// <summary>
        /// Handles the action of adding a new assignment profile into the database.
        /// </summary>
        /// <param name="assignmentProfileDto">The new assignment profile dto</param>
        /// <returns></returns>
        Task<long> AddAssignmentProfileAsync(NewAssignmentProfileDto assignmentProfileDto);

        /// <summary>
        /// Handles the action of removing an assignment profile from the database.
        /// </summary>
        /// <param name="assignmentId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        Task<int> RemoveAssignmentProfileAsync(long[] assignmentIds);

        /// <summary>
        /// Handles the action of copying an assignment profile and adding it into the database.
        /// </summary>
        /// <param name="assignmentProfileIds">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        Task<int> CopyAssignmentProfileAsync(long[] assignmentProfileIds);

        /// <summary>
        /// Handles the action of editing an assignment profile.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="newAssignmentProfileDto">The assignment profile dto to be edited</param>
        /// <returns></returns>
        Task<int> EditAssignmentProfileAsync(long assignmentProfileId, NewAssignmentProfileDto newAssignmentProfileDto);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a private application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignAssignmentProfileToPrivateApplicationsAsync(long assignmentProfileId, int[] applicationIds);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a public application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignAssignmentProfileToPublicApplicationsAsync(long assignmentProfileId, int[] applicationIds);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a private application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignAssignmentProfileToPrivateApplicationsJobAsync(UserDto loggedInUser, Guid subscriptionId, long assignmentProfileId, int[] applicationIds);

        /// <summary>
        /// Handles the action of assigning an assignment profile to a public application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> AssignAssignmentProfileToPublicApplicationsJobAsync(UserDto loggedInUser, Guid subscriptionId, long assignmentProfileId, int[] applicationIds);

        /// <summary>
        /// Handles the action of clearing the assignment profile from the private applications.
        /// </summary>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Task<int> ClearAssignedProfilesFromPrivateApplicationsAsync(int[] applicationIds);

        /// <summary>
        /// Handles the action of clearing the assignment profiles from public applications.
        /// </summary>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        Task<int> ClearAssignedProfilesFromPublicApplicationsAsync(int[] applicationIds);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggedInUser"></param>
        /// <param name="assignmentProfile"></param>
        /// <param name="applications"></param>
        /// <param name="assignmentProfileGroups"></param>
        /// <returns></returns>
        Task AssignAssignmentProfileToApplicationsNoCheckAsync(UserDto loggedInUser, AssignmentProfile assignmentProfile, AssignmentProfileApplicationDto[] applications, AssignmentProfileGroup[] assignmentProfileGroups, bool generateNotifications = true);
    }
}
