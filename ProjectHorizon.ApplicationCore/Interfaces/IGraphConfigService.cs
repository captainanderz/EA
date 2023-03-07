using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IGraphConfigService
    {
        Task CreateGraphConfigAsync(GraphConfigDto graphConfigDto, Guid subscriptionId);

        Task RemoveGraphConfigAsync(Guid subscriptionId);

        Task<bool> HasGraphConfigAsync(Guid subscriptionId);

        Task<GraphConfigDto> GetGraphConfigAsync(Guid subscriptionId);

        Task CheckGraphStatusAsync(Guid subscriptionId);
    }
}
