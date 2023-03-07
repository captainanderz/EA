using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAzureUserService
    {
        Task<IEnumerable<AzureUserDto>> GetUsersDirectReportsAsync(Guid subscriptionId, string managerId);
        Task<AzureUserDto?> GetUserManagerAsync(Guid subscriptionId, string userId);

        Task<AzureUserDto?> GetUserAsync(Guid subscriptionId, string userId);
    }
}