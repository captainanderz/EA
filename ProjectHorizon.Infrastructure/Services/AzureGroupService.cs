using AutoMapper;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class AzureGroupService : IAzureGroupService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IGraphConfigService _graphConfigService;
        private readonly IMapper _mapper;

        public AzureGroupService(IApplicationDbContext applicationDbContext, IMapper mapper, IGraphConfigService graphConfigService)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _graphConfigService = graphConfigService;
        }

        public async Task<AzureGroupDto> AddAzureGroupAsync(Guid subscriptionId, string groupName)
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

            Group? group = await graphServiceClient.Groups
                .Request()
                .AddAsync(new Group
                {
                    DisplayName = groupName,
                    MailEnabled = false,
                    MailNickname = $"{Guid.NewGuid()}",
                    SecurityEnabled = true,
                    GroupTypes = new List<string>()
                    {
                    },
                });

            AzureGroup? entity = new AzureGroup
            {
                Id = group.Id,
                Name = group.DisplayName,
                Mail = group.Mail
            };

            AzureGroupDto? dto = new AzureGroupDto
            {
                Id = group.Id,
                DisplayName = group.DisplayName,
                Mail = group.Mail
            };

            _applicationDbContext.AzureGroups.Add(entity);
            await _applicationDbContext.SaveChangesAsync();

            return dto;
        }

        public async Task AddUserToGroupAsync(Guid subscriptionId, string groupId, string userId)
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

            DirectoryObject? directoryObject = new DirectoryObject
            {
                Id = userId
            };

            await graphServiceClient.Groups[groupId]
                .Members
                .References
                .Request()
                .AddAsync(directoryObject);
        }

        public async Task<CursorPagedResult<AzureGroupDto>> FilterAzureGroupsByNameAsync(Guid subscriptionId, string? groupName, string? nextPageLink)
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

            var queryOptions = new List<QueryOption>()
            {
                new QueryOption("$count", "true")
            };

            IGraphServiceGroupsCollectionRequest? groupsRequest = graphServiceClient.Groups.Request(queryOptions);

            if (!string.IsNullOrEmpty(groupName))
            {
                queryOptions.Add(new QueryOption("$search", $"\"displayName:{groupName}\""));
                groupsRequest = graphServiceClient.Groups.Request(queryOptions);
            }

            groupsRequest = groupsRequest.Header("ConsistencyLevel", "eventual");

            IGraphServiceGroupsCollectionPage? groups = await groupsRequest.OrderBy("displayName").GetAsync();

            if (!string.IsNullOrEmpty(nextPageLink))
            {
                groups.InitializeNextPageRequest(graphServiceClient, nextPageLink.DecodeBase64());
                groups = await groups.NextPageRequest.GetAsync();
            }

            var result = new CursorPagedResult<AzureGroupDto>
            {
                AllItemsCount = -1,
                PageItems = groups.Select(group => new AzureGroupDto
                {
                    Id = group.Id,
                    DisplayName = group.DisplayName,
                    Mail = group.Mail
                })
            };
            
            if (groups.NextPageRequest is not null)
            {
                var link = groups.NextPageRequest.GetHttpRequestMessage().RequestUri.ToString();
                result.NextPageLink = link.EncodeBase64();
            }

            return result;
        }

        public async Task<IEnumerable<AzureGroupDto>> GetGroupsByIdsAsync(Guid subscriptionId, Guid[] groupIds)
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

            IGraphServiceGroupsCollectionRequest? groupsRequest = graphServiceClient.Groups.Request();

            IGraphServiceGroupsCollectionPage? groups = await groupsRequest.GetAsync();

            string[] groupIdQueries = groupIds.Select(groupId => $"'{groupId}'").ToArray();

            string groupIdsQuery = $"({string.Join(",", groupIdQueries)})";

            groupsRequest.Filter($"id in {groupIdsQuery}");

            return groups.Select(group => new AzureGroupDto
            {
                Id = group.Id,
                DisplayName = group.DisplayName,
                Mail = group.Mail
            });
        }

        public async Task DeleteGroupAsync(Guid subscriptionId, string groupId)
        {
            IAuthenticationProvider? clientCredentialsAuthProvider = null;

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

            await graphServiceClient.Groups[groupId]
                .Request()
                .DeleteAsync();
        }
    }
}