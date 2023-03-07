using ProjectHorizon.ApplicationCore.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class NewAssignmentProfileDto : IValidatableObject
    {
        public string Name { get; set; }

        public AssignmentProfileGroupDto[] Groups { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Check if there's multiple included AssignmentProfileGroups with the same azure group id
            bool multipleIncludedForTheSameAzureGroupId = Groups.Any(
                group =>
                {
                    int includedGroupsWithTheSameAzureGroupId = Groups
                    .Count(otherGroup => otherGroup.GroupModeId == GroupMode.Included &&
                        otherGroup.AzureGroupId == group.AzureGroupId);

                    return includedGroupsWithTheSameAzureGroupId > 1;
                }
            );

            if (multipleIncludedForTheSameAzureGroupId)
            {
                yield return new ValidationResult($"Cannot have the same group included more than once.", new[] { nameof(Groups) });
            }

            // Check if there's multiple assignments of type AllUsers
            bool multipleAllUsers = Groups.Count(group => group.GroupModeId is GroupMode.AllUsers) > 1;

            if (multipleAllUsers)
            {
                yield return new ValidationResult($"Cannot have multiple AllUsers assignment.", new[] { nameof(Groups) });
            }

            // Check if there's multiple assignments of type AllDevices
            bool multipleAllDevices = Groups.Count(group => group.GroupModeId is GroupMode.AllDevices) > 1;

            if (multipleAllDevices)
            {
                yield return new ValidationResult($"Cannot have multiple AllDevices assignment.", new[] { nameof(Groups) });
            }

            // Check if there's an AllDevices assignment in the Available assignment type
            bool allDevicesInAvailable = Groups.Any(group => group.GroupModeId is GroupMode.AllDevices && group.AssignmentTypeId is AssignmentType.Available);

            if (allDevicesInAvailable)
            {
                yield return new ValidationResult($"Cannot have AllDevices assigned in the Available assignment type.", new[] { nameof(Groups) });
            }
        }
    }
}