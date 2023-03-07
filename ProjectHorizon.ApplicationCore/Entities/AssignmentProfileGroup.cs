using ProjectHorizon.ApplicationCore.Enums;
using System;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class AssignmentProfileGroup : BaseEntity
    {
        /// <summary>
        /// This is just because Paul doesn't search on google things before doing database diagrams
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The <see cref="AssignmentProfile"/> to which this group belongs.
        /// </summary>
        public virtual AssignmentProfile AssignmentProfile { get; set; }

        /// <summary>
        /// Gets or sets the ID of the <see cref="AssignmentProfile"/> this group belongs to.
        /// </summary>
        public long AssignmentProfileId { get; set; }

        /// <summary>
        /// Gets or sets the value of the assignment mode <see cref="Enums.AssignmentType"/>.
        /// </summary>
        public AssignmentType AssignmentTypeId { get; set; }

        /// <summary>
        /// The ID of the group, defined in Intune.
        /// </summary>
        public Guid? AzureGroupId { get; set; }

        /// <summary>
        /// The name of the group, defined in Intune.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the value of the <see cref="Enums.DeliveryOptimizationPriority"/> enum, indicating which delivery optimization priority mode was selected.
        /// </summary>
        public DeliveryOptimizationPriority DeliveryOptimizationPriorityId { get; set; }

        /// <summary>
        /// Gets or sets the value of the <see cref="Enums.EndUserNotification"/> enum, indicating which end user notification mode was selected.
        /// </summary>
        public EndUserNotification EndUserNotificationId { get; set; }

        /// <summary>
        /// Gets or sets the id of the Intune filter.
        /// </summary>
        public Guid FilterId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the <see cref="Enums.FilterMode"/> enum.
        /// When editing an application in Intune and assigning groups in 3 categories, <see cref="AssignmentTypeId"/> (Required, Available and Uninstall),
        /// a single filter can be assigned to each group, using the selected mode.
        /// </summary>
        public FilterMode FilterModeId { get; set; }

        /// <summary>
        /// Gets or sets the value of the group mode <see cref="Enums.GroupMode"/>.
        /// </summary>
        public GroupMode GroupModeId { get; set; }
    }
}