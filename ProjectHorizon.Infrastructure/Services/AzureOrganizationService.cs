using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class AzureOrganizationService : IAzureOrganizationService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IGraphConfigService _graphConfigService;

        public AzureOrganizationService(IApplicationDbContext applicationDbContext, IGraphConfigService graphConfigService)
        {
            _applicationDbContext = applicationDbContext;
            _graphConfigService = graphConfigService;
        }

        public async Task<OrganizationDto> GetAsync(Guid subscriptionId)
        {
            IAuthenticationProvider? clientCredentialsAuthProvider = null;
            GraphConfigDto? graphConfigDto = null;

            if (await _graphConfigService.HasGraphConfigAsync(subscriptionId))
            {
                graphConfigDto = await _graphConfigService.GetGraphConfigAsync(subscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }

            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);
            var organization = await graphServiceClient
                .Organization[graphConfigDto.Tenant]
                .Request()
                .GetAsync();

            return new OrganizationDto
            {
                Id = organization.Id,
                DisplayName = organization.DisplayName,
            };
        }
    }
}
