using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class GraphConfigService : IGraphConfigService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly JwtSecurityTokenHandler _jwtTokenHandler = new JwtSecurityTokenHandler();
        private readonly IAuditLogService _auditLogService;
        private readonly GraphConfigEncryption _graphConfigEncryption;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        public GraphConfigService(
            IApplicationDbContext applicationDbContext,
            IAuditLogService auditLogService,
            IOptions<GraphConfigEncryption> graphConfigEncryption,
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationDbContext = applicationDbContext;
            _auditLogService = auditLogService;
            _graphConfigEncryption = graphConfigEncryption.Value;
            _loggedInUserProvider = loggedInUserProvider;
        }

        public async Task CreateGraphConfigAsync(GraphConfigDto graphConfigDto, Guid subscriptionId)
        {
            Subscription? subscription = await _applicationDbContext
                .Subscriptions
                .SingleAsync(sub => sub.Id == subscriptionId);

            subscription.GraphConfig = new GraphConfig
            {
                ClientId = graphConfigDto.ClientId,
                ClientSecret = graphConfigDto.ClientSecret.EncryptAES(_graphConfigEncryption.Key),
                Tenant = graphConfigDto.Tenant
            };

            await _applicationDbContext.SaveChangesAsync();

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.Integrations,
                string.Format(AuditLogActions.GraphConfigCreated));
        }

        public async Task<GraphConfigDto> GetGraphConfigAsync(Guid subscriptionId)
        {
            Subscription? subscription = await _applicationDbContext
                .Subscriptions
                .SingleAsync(sub => sub.Id == subscriptionId);

            GraphConfigDto? graphConfigDto = new GraphConfigDto
            {
                ClientId = subscription.GraphConfig.ClientId,
                ClientSecret = subscription.GraphConfig.ClientSecret.DecryptAES(_graphConfigEncryption.Key),
                Tenant = subscription.GraphConfig.Tenant
            };

            return graphConfigDto;
        }

        public async Task RemoveGraphConfigAsync(Guid subscriptionId)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Subscription? subscription = await _applicationDbContext
                .Subscriptions
                .SingleAsync(sub => sub.Id == subscriptionId);

            _applicationDbContext.GraphConfigs.Remove(subscription.GraphConfig);

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.Integrations,
                string.Format(AuditLogActions.GraphConfigRemoved, subscription.Name),
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<bool> HasGraphConfigAsync(Guid subscriptionId)
        {
            Subscription? subscription = await _applicationDbContext
                .Subscriptions
                .SingleAsync(sub => sub.Id == subscriptionId);

            return subscription.GraphConfig != null;
        }

        public async Task CheckGraphStatusAsync(Guid subscriptionId)
        {
            GraphConfigDto? graphConfigDto = await GetGraphConfigAsync(subscriptionId);
            GraphAuthProvider? clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            string authResult = await clientCredentialsAuthProvider.GetAccessToken();

            ValidateToken(authResult);
        }

        public void ValidateToken(string authResult)
        {
            List<string> requiredClaims = new List<string>()
            {
                "Application.ReadWrite.OwnedBy",
                "Application.ReadWrite.All",
                "Application.Read.All",
                "DeviceManagementApps.ReadWrite.All",
                "DeviceManagementApps.Read.All",
                "DeviceManagementManagedDevices.Read.All"
            };

            JwtSecurityToken jwt = _jwtTokenHandler.ReadJwtToken(authResult);

            foreach (System.Security.Claims.Claim? claim in jwt.Claims)
            {
                if (requiredClaims.Contains(claim.Value))
                {
                    requiredClaims.Remove(claim.Value);
                }
            }

            if (requiredClaims.Any())
            {
                throw new MsalServiceException("missing_claims", $"Missing required client permissions {string.Join(",", requiredClaims)}");
            }
        }
    }
}
