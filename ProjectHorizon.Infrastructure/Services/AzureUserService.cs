using AutoMapper;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class AzureUserService : IAzureUserService
    {
        private readonly IGraphConfigService _graphConfigService;
        private readonly IMapper _mapper;

        public AzureUserService(
            IGraphConfigService graphConfigService,
            IMapper mapper
            )
        {
            _graphConfigService = graphConfigService;
            _mapper = mapper;
        }

        public async Task<AzureUserDto?> GetUserManagerAsync(Guid subscriptionId, string userId)
        {
            IAuthenticationProvider clientCredentialsAuthProvider = null;

            if (await _graphConfigService.HasGraphConfigAsync(subscriptionId))
            {
                GraphConfigDto graphConfigDto = await _graphConfigService.GetGraphConfigAsync(subscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }

            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);

            try
            {

                DirectoryObject? managerDirectoryObject = await graphServiceClient
                    .Users[userId]
                    .Manager
                    .Request()
                    .GetAsync();

                User? manager = await graphServiceClient
                    .Users[managerDirectoryObject.Id]
                    .Request()
                    .GetAsync();

                return new AzureUserDto
                {
                    Id = manager.Id,
                    Name = manager.DisplayName,
                    Email = manager.Mail,
                };
            }
            catch (ServiceException)
            {
                return null;
            }
        }

        public async Task<AzureUserDto?> GetUserAsync(Guid subscriptionId, string userId)
        {
            IAuthenticationProvider clientCredentialsAuthProvider = null;

            if (await _graphConfigService.HasGraphConfigAsync(subscriptionId))
            {
                GraphConfigDto graphConfigDto = await _graphConfigService.GetGraphConfigAsync(subscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }   

            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);

            try
            {
                User? user = await graphServiceClient
                    .Users[userId]
                    .Request()
                    .GetAsync();

                return new AzureUserDto
                {
                    Id = user.Id,
                    Name = user.DisplayName,
                    Email = user.Mail,
                };
            }
            catch (ServiceException)
            {
                return null;
            }
        }

        //Get users subordinates
        public async Task<IEnumerable<AzureUserDto>> GetUsersDirectReportsAsync(Guid subscriptionId, string managerId)
        {
            IAuthenticationProvider clientCredentialsAuthProvider = null;

            if (await _graphConfigService.HasGraphConfigAsync(subscriptionId))
            {
                GraphConfigDto graphConfigDto = await _graphConfigService.GetGraphConfigAsync(subscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }

            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);

            List<DirectoryObject> users = new List<DirectoryObject>();
            IUserDirectReportsCollectionWithReferencesPage? usersWithThisManager = await graphServiceClient
                .Users[managerId]
                .DirectReports
                .Request()
                .GetAsync();

            users.AddRange(usersWithThisManager);

            return users.Select(user => new AzureUserDto
            {
                Id = user.Id,
            });
        }
    }
}