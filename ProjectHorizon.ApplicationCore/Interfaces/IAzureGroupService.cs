using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAzureGroupService
    {
        Task<CursorPagedResult<AzureGroupDto>> FilterAzureGroupsByNameAsync(Guid subscriptionId, string? groupName, string? nextPageLink);

        Task<IEnumerable<AzureGroupDto>> GetGroupsByIdsAsync(Guid subscriptionId, Guid[] groupIds);

        Task<AzureGroupDto> AddAzureGroupAsync(Guid subscriptionId, string groupName);

        Task AddUserToGroupAsync(Guid subscriptionId, string groupId, string userId);

        Task DeleteGroupAsync(Guid subscriptionId, string groupId);
    }
}
