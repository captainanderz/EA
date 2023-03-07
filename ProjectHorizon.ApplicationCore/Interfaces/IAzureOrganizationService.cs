using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAzureOrganizationService
    {
        Task<OrganizationDto> GetAsync(Guid subscriptionId);
    }
}
