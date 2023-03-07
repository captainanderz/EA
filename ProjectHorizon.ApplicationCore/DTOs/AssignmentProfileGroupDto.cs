using ProjectHorizon.ApplicationCore.Enums;
using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class AssignmentProfileGroupDto
    {
        public Guid? AzureGroupId { get; set; }

        public string DisplayName { get; set; }

        public AssignmentType AssignmentTypeId { get; set; }

        public DeliveryOptimizationPriority DeliveryOptimizationPriorityId { get; set; }

        public EndUserNotification EndUserNotificationId { get; set; }

        public GroupMode GroupModeId { get; set; }
    }
}
