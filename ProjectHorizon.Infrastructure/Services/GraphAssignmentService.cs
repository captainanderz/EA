using log4net;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class GraphAssignmentService : IGraphAssignmentService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IGraphConfigService _graphConfigService;

        public GraphAssignmentService(IGraphConfigService graphConfigService)
        {
            _graphConfigService = graphConfigService;
        }

        public async Task SetMobileAppAssignmentAsync(AssignmentProfileApplicationDto application, AssignmentProfileGroupDto[] assignmentProfileGroupDtos)
        {
            IAuthenticationProvider? clientCredentialsAuthProvider = null;

            if (await _graphConfigService.HasGraphConfigAsync(application.SubscriptionId))
            {
                GraphConfigDto graphConfigDto = await _graphConfigService.GetGraphConfigAsync(application.SubscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }

            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);

            System.Collections.Generic.IEnumerable<MobileAppAssignment>? appAssignments = assignmentProfileGroupDtos.Select(assignmentProfileGroupDto =>
            {
                return new MobileAppAssignment
                {
                    Intent = ((AssignmentType)assignmentProfileGroupDto.AssignmentTypeId).ToInstallIntent(),
                    Settings = assignmentProfileGroupDto.GroupModeId switch
                    {
                        GroupMode.NotSet => throw new NotImplementedException(),
                        GroupMode.Included or GroupMode.AllUsers or GroupMode.AllDevices => new Win32LobAppAssignmentSettings
                        {
                            DeliveryOptimizationPriority = assignmentProfileGroupDto.DeliveryOptimizationPriorityId.ToWin32LobAppDeliveryOptimizationPriority(),
                            Notifications = assignmentProfileGroupDto.EndUserNotificationId.ToWin32LobAppNotification()
                        },
                        GroupMode.Excluded => null,
                        _ => throw new NotImplementedException(),
                    },
                    Target = assignmentProfileGroupDto.GroupModeId switch
                    {
                        GroupMode.NotSet => throw new NotImplementedException(),
                        GroupMode.Included => new GroupAssignmentTarget { GroupId = assignmentProfileGroupDto.AzureGroupId.ToString() },
                        GroupMode.Excluded => new ExclusionGroupAssignmentTarget { GroupId = assignmentProfileGroupDto.AzureGroupId.ToString() },
                        GroupMode.AllUsers => new AllLicensedUsersAssignmentTarget(),
                        GroupMode.AllDevices => new AllDevicesAssignmentTarget(),
                        _ => throw new NotImplementedException(),
                    },
                };
            });

            await graphServiceClient.DeviceAppManagement
                .MobileApps[application.IntuneId]
                .Assign(appAssignments)
                .Request()
                .PostAsync();
        }
    }
}
