using AutoMapper;
using AutoMapper.QueryableExtensions;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class AssignmentProfileService : IAssignmentProfileService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IGraphAssignmentService _graphAssignmentService;
        private readonly IAzureGroupService _azureGroupService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        public AssignmentProfileService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IGraphAssignmentService graphAssignmentService,
            IAzureGroupService azureGroupService,
            IBackgroundJobService backgroundJobService,
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _graphAssignmentService = graphAssignmentService;
            _azureGroupService = azureGroupService;
            _backgroundJobService = backgroundJobService;
            _loggedInUserProvider = loggedInUserProvider;
        }

        /// <summary>
        /// Handles the action of paginating the assignment profiles.
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <returns></returns>
        public async Task<PagedResult<AssignmentProfileDto>> ListAssignmentProfilesPagedAsync(int pageNumber, int pageSize, string searchTerm)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get all the assignment profiles that the given subscription contains
            IQueryable<AssignmentProfile>? queryAssignmentProfiles = _applicationDbContext
                .AssignmentProfiles
                .Where(assignmentProfile => assignmentProfile.SubscriptionId == subscriptionId);

            // Filter the assignment profiles shown by the input of the search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryAssignmentProfiles = queryAssignmentProfiles
                    .Where(assignmentProfile => assignmentProfile.Name.Contains(searchTerm.Trim()));
            }

            // Take all the assignment profiles from this subscription order them based on ModifiedOn and Name property an do the paginating
            List<AssignmentProfile>? assignmentProfiles = await queryAssignmentProfiles
                .OrderByDescending(assignmentProfile => assignmentProfile.ModifiedOn)
                .ThenBy(assignmentProfile => assignmentProfile.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(assignmentProfile => assignmentProfile.DeploymentSchedules)
                .ToListAsync();

            List<AssignmentProfileDto>? assignmentProfileDtos = _mapper.Map<List<AssignmentProfileDto>>(assignmentProfiles);

            // Take all assignment profiles on the page
            for (int i = 0; i < assignmentProfiles.Count; i++)
            {
                var assignmentProfile = assignmentProfiles[i];
                var assignmentProfileDto = assignmentProfileDtos[i];

                // Count the private applications that have this profile assigned
                int assignedPrivateApplications = await _applicationDbContext
                    .PrivateApplications
                    .CountAsync(privateApplication => privateApplication.AssignmentProfileId.HasValue && privateApplication.AssignmentProfileId == assignmentProfile.Id);

                // Count the public applications that have this profile assigned
                int assignedPublicApplications = await _applicationDbContext
                    .SubscriptionPublicApplications
                    .CountAsync(subscriptionPublicApplication => subscriptionPublicApplication.AssignmentProfileId.HasValue && subscriptionPublicApplication.AssignmentProfileId == assignmentProfile.Id);

                // Set the number of applications assigned of this profile to be the sum between the number of private applications
                // assigned to this profile and the number of public applications assigned to this profile
                assignmentProfileDto.NumberOfApplicationsAssigned = assignedPrivateApplications + assignedPublicApplications;
                assignmentProfileDto.NumberOfDeploymentSchedules = assignmentProfile.DeploymentSchedules.Count;
            }

            return new PagedResult<AssignmentProfileDto>
            {
                AllItemsCount = await queryAssignmentProfiles.CountAsync(),
                PageItems = assignmentProfileDtos
            };
        }

        /// <summary>
        /// Handles the action of getting a list with all the assignment profile ids
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<long>> ListAssignmentProfileIdsAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            return await _applicationDbContext
                .AssignmentProfiles
                .Where(assignmentProfile => assignmentProfile.SubscriptionId == subscriptionId)
                .Select(assignmentProfile => assignmentProfile.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Handles the action of returning an assignment profile by a given id.
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AssignmentProfileDetailsDto> GetAssignmentProfileAsync(long assignmentProfileId)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get the assignment profile we want by the given id
            AssignmentProfile? assignmentProfile = await _applicationDbContext.AssignmentProfiles
                .Where(assignment => assignment.Id == assignmentProfileId && assignment.SubscriptionId == subscriptionId)
                .FirstOrDefaultAsync();

            // Check if the profile exists
            if (assignmentProfile == null)
            {
                _log.Error($"Assignment profile with the id:{assignmentProfileId}, does not exist");
                throw new Exception($"Assignment profile with the id:{assignmentProfileId}, does not exist");
            }

            // Get all the groups corresponding to the assignment profile we want to get
            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
               .Where(group => group.AssignmentProfileId == assignmentProfileId)
               .ToArrayAsync();

            // Get all the azure group ids
            Guid[]? azureGroupIds = assignmentProfileGroups
               .Where(group => group.AzureGroupId.HasValue)
               .Select(group => group.AzureGroupId!.Value)
               .Distinct()
               .ToArray();

            // Take the groups from intune and store them into an array

            // Create a new array of assignment profile group details with the length equal to that of the groups that correspond to the assignment profile we want
            AssignmentProfileGroupDetailsDto[] assignmentProfileGroupDetailsDtos = new AssignmentProfileGroupDetailsDto[assignmentProfileGroups.Length];

            for (int i = 0; i < assignmentProfileGroupDetailsDtos.Length; i++)
            {
                AssignmentProfileGroup? assignmentProfileGroup = assignmentProfileGroups[i];
                string? name = assignmentProfileGroup.AzureGroupId switch
                {
                    Guid azureGroupId => assignmentProfileGroups[i].DisplayName,
                    _ => assignmentProfileGroup.GroupModeId switch
                    {
                        GroupMode groupMode when groupMode is GroupMode.AllUsers or GroupMode.AllDevices => Enum.GetName(typeof(GroupMode), groupMode),
                        _ => string.Empty,
                    }
                };

                // For every group in the assignment profiles group details array, we create a dto from the assignment profile group entity
                assignmentProfileGroupDetailsDtos[i] = new AssignmentProfileGroupDetailsDto
                {
                    AssignmentTypeId = assignmentProfileGroups[i].AssignmentTypeId,
                    DeliveryOptimizationPriorityId = assignmentProfileGroups[i].DeliveryOptimizationPriorityId,
                    EndUserNotificationId = assignmentProfileGroups[i].EndUserNotificationId,
                    AzureGroupId = assignmentProfileGroups[i].AzureGroupId,
                    DisplayName = name,
                    GroupModeId = assignmentProfileGroups[i].GroupModeId
                };
            }

            AssignmentProfileDetailsDto? assignmentProfileDetails = _mapper.Map<AssignmentProfileDetailsDto>(assignmentProfile);

            assignmentProfileDetails.Groups = assignmentProfileGroupDetailsDtos;

            return assignmentProfileDetails;
        }

        /// <summary>
        /// Handles the action of filtering of all assignment profiles, by a given name.
        /// </summary>
        /// <param name="assignmentProfileName">The name of the assignment profile we want to filter by</param>
        /// <returns></returns>
        public async Task<IEnumerable<AssignmentProfileDto>> FilterAssignmentProfilesByNameAsync(string? assignmentProfileName)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get all the assignment profiles of the subscription
            IQueryable<AssignmentProfile>? assignmentProfiles = _applicationDbContext.AssignmentProfiles
                .Where(assignmentProfile => assignmentProfile.SubscriptionId == subscriptionId); ;

            // If the users types something in search box, return the assignment profiles that name starts with the letter the user typed and then order
            // them by name
            if (!string.IsNullOrEmpty(assignmentProfileName))
            {
                return await assignmentProfiles
                    .Where(assignmentProfile => assignmentProfile.Name.ToLower().Contains(assignmentProfileName.ToLower()))
                    .OrderBy(assignmentProfile => assignmentProfile.Name)
                    .ProjectTo<AssignmentProfileDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
            }

            // If there is no search term than return all the assignment profiles order by name
            return await assignmentProfiles
                .OrderBy(assignmentProfile => assignmentProfile.Name)
                .ProjectTo<AssignmentProfileDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Handles the action of adding a new assignment profile into the database.
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="newAssignmentProfileDto">The new assignment profile dto</param>
        /// <returns></returns>
        public async Task<long> AddAssignmentProfileAsync(NewAssignmentProfileDto newAssignmentProfileDto)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            AssignmentProfile? newAssignmentProfile = _mapper.Map<AssignmentProfile>(newAssignmentProfileDto);

            // Set the SubscriptionId of the new assignment profile to the current subscription
            newAssignmentProfile.SubscriptionId = subscriptionId;

            AssignmentProfileGroup[]? groups = _mapper.Map<AssignmentProfileGroup[]>(newAssignmentProfileDto.Groups);

            // Add the new assignment profile into the database
            newAssignmentProfile = _applicationDbContext.AssignmentProfiles.Add(newAssignmentProfile).Entity;

            await _applicationDbContext.SaveChangesAsync();

            // Add the selection groups of the new assignment profile into the database
            for (int i = 0; i < groups.Length; i++)
            {
                AssignmentProfileGroup? group = groups[i];
                group.AssignmentProfileId = newAssignmentProfile.Id;
                group.DeliveryOptimizationPriorityId = newAssignmentProfileDto.Groups[i].DeliveryOptimizationPriorityId;
                group.EndUserNotificationId = newAssignmentProfileDto.Groups[i].EndUserNotificationId;

                _applicationDbContext.AssignmentProfileGroups.Add(group);
            }

            // Show notifications in audit log
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.AssignmentProfiles,
                string.Format(AuditLogActions.AssignmentProfilesAddProfile, newAssignmentProfileDto.Name),
                author: loggedInUser,
                saveChanges: false);

            await _applicationDbContext.SaveChangesAsync();

            _log.Info($"Assignment profile '{newAssignmentProfile.Name}' with the id: {newAssignmentProfile.Id} was added.");

            return newAssignmentProfile.Id;
        }

        /// <summary>
        /// Handles the action of removing an assignment profile from the database.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="assignmentId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        public async Task<int> RemoveAssignmentProfileAsync(long[] assignmentIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the assignment profiles we want to delete
            AssignmentProfile[] assignmentProfilesToBeDeleted = await _applicationDbContext
                .AssignmentProfiles
                .Where(assignmentProfile => assignmentIds.Contains(assignmentProfile.Id))
                .ToArrayAsync();

            // Check if the profile exists
            if (assignmentProfilesToBeDeleted.Length != assignmentIds.Length)
            {
                _log.Error($"One ore more assignment profile ids are invalid.");
                return StatusCodes.Status400BadRequest;
            }

            // Check if it the profile is from the current subscription
            foreach (AssignmentProfile? assignmentProfile in assignmentProfilesToBeDeleted)
            {
                if (assignmentProfile.SubscriptionId != loggedInUser.SubscriptionId)
                {
                    _log.Error($"The assignment profile with the id: {assignmentProfile.Id}, is not from the current subscription");
                    return StatusCodes.Status401Unauthorized;
                }
            }

            // Get all private applications that have an assignment profile that has the id in the assignment profiles to be deleted array assigned to them
            List<PrivateApplication>? privateApplications = await _applicationDbContext.PrivateApplications
                .Where(privateApplication => privateApplication.AssignmentProfileId != null && assignmentIds.Contains(privateApplication.AssignmentProfileId!.Value))
                .ToListAsync();

            // Delete the connection between the private applications and the assignment profile by making the AssignmentProfileId null
            foreach (PrivateApplication? privateApplication in privateApplications)
            {
                privateApplication.AssignmentProfileId = null;
            }

            // Get all public applications that have this assignment profile assigned to them
            List<SubscriptionPublicApplication>? subscriptionPublicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.AssignmentProfileId != null && assignmentIds.Contains(subscriptionPublicApplication.AssignmentProfileId!.Value))
                .ToListAsync();

            // Get all intune ids that correspond to the private applications than combine them with the public applications intune ids and then
            // get only the ones that are not null
            IEnumerable<AssignmentProfileApplicationDto>? publicAssignmentProfileApplicationDtos = subscriptionPublicApplications
               .Where(subscriptionPublicApplication => subscriptionPublicApplication.IntuneId is not null)
               .Select(subscriptionPublicApplication => new AssignmentProfileApplicationDto
               {
                   SubscriptionId = subscriptionPublicApplication.SubscriptionId,
                   IntuneId = subscriptionPublicApplication.IntuneId,
                   Name = subscriptionPublicApplication.PublicApplication.Name,
               });

            IEnumerable<AssignmentProfileApplicationDto>? privateAssignmentProfileApplicationDtos = privateApplications
                .Where(application => application.IntuneId is not null)
                .Select(application => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = application.SubscriptionId,
                    IntuneId = application.IntuneId,
                    Name = application.Name,
                });

            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = publicAssignmentProfileApplicationDtos
                .Concat(privateAssignmentProfileApplicationDtos)
                .ToArray();

            ClearAssignedProfiles(loggedInUser, assignmentProfileApplicationDtos);

            // Delete the connection between the public applications and the assignment profile by making the AssignmentProfileId null
            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                subscriptionPublicApplication.AssignmentProfileId = null;
            }

            // Get all the groups that have the assignment profile we want to delete
            List<AssignmentProfileGroup>? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(assignmentProfileGroup => assignmentIds.Contains(assignmentProfileGroup.AssignmentProfileId))
                .ToListAsync();

            // Delete all the groups that have a connection with the assignment profile we want to delete
            foreach (AssignmentProfileGroup? assignmentProfileGroup in assignmentProfileGroups)
            {
                _applicationDbContext.AssignmentProfileGroups.Remove(assignmentProfileGroup);
            }

            foreach (AssignmentProfile? assignmentProfile in assignmentProfilesToBeDeleted)
            {
                _applicationDbContext.AssignmentProfiles.Remove(assignmentProfile);
            }

            await _applicationDbContext.SaveChangesAsync();

            string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

            foreach (AssignmentProfile? assignmentProfile in assignmentProfilesToBeDeleted)
            {
                // Show notifications in audit log
                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.AssignmentProfiles,
                    string.Format(AuditLogActions.AssignmentProfilesRemoveProfile, assignmentProfile.Name),
                    author: loggedInUser,
                    saveChanges: false
                    );

                _log.Info($"Remove assignment profile '{assignmentProfile.Name}' with id: {assignmentProfile.Id}.");
            }

            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of copying an assignment profile and adding it into the database.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <returns></returns>
        public async Task<int> CopyAssignmentProfileAsync(long[] assignmentProfileIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the assignment profile that we want to copy
            AssignmentProfile[]? assignmentProfilesToCopy = await _applicationDbContext.AssignmentProfiles
                .Where(assignmentProfile => assignmentProfileIds.Contains(assignmentProfile.Id)).ToArrayAsync();

            // Check if the profile is valid
            if (assignmentProfilesToCopy.Length != assignmentProfileIds.Length)
            {
                _log.Error($"One ore more assignment profile ids are invalid.");
                return StatusCodes.Status400BadRequest;
            }

            List<EntityEntry<AssignmentProfile>>? newAssignmentProfiles = new List<EntityEntry<AssignmentProfile>>(assignmentProfilesToCopy.Length);

            foreach (AssignmentProfile? assignmentProfile in assignmentProfilesToCopy)
            {
                var name = assignmentProfile.Name + Utils.CopySuffix;

                if (name.Length > Validation.NameMaxLength)
                {
                    name = assignmentProfile.Name;
                }

                // Create a new empty assignment profile and copy the properties of the assignment profile that we want to copy
                AssignmentProfile? copiedAssignmentProfile = new AssignmentProfile
                {
                    Name = name,
                    SubscriptionId = assignmentProfile.SubscriptionId,
                };

                EntityEntry<AssignmentProfile>? newAssignmentProfile = _applicationDbContext.AssignmentProfiles.Add(copiedAssignmentProfile);
                newAssignmentProfiles.Add(newAssignmentProfile);
            }

            await _applicationDbContext.SaveChangesAsync();

            var assignmentProfilesNewAndOriginal = newAssignmentProfiles.Zip(
                assignmentProfilesToCopy,
                (newAssignmentProfile, originalAssignmentProfile) => new { NewAssignmentProfile = newAssignmentProfile, OriginalAssignmentProfile = originalAssignmentProfile }
                );

            foreach (var assignmentProfileNewAndOriginal in assignmentProfilesNewAndOriginal)
            {
                AssignmentProfile? newAssignmentProfile = assignmentProfileNewAndOriginal.NewAssignmentProfile.Entity;
                AssignmentProfile? originalAssignmentProfile = assignmentProfileNewAndOriginal.OriginalAssignmentProfile;

                // Get the groups of the assignment profile that we want to copy and store them into a variable
                List<AssignmentProfileGroup>? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                    .Where(group => group.AssignmentProfileId == originalAssignmentProfile.Id)
                    .ToListAsync();

                // Loop through each group of the assignment profile we want to copy, copy them, and add them in the db
                foreach (AssignmentProfileGroup? group in assignmentProfileGroups)
                {
                    // Copy the groups of the assignment profile we want to copy
                    AssignmentProfileGroup? groupToBeCopied = new AssignmentProfileGroup
                    {
                        AssignmentProfileId = newAssignmentProfile.Id,
                        AssignmentTypeId = group.AssignmentTypeId,
                        AzureGroupId = group.AzureGroupId,
                        DisplayName = group.DisplayName,
                        DeliveryOptimizationPriorityId = group.DeliveryOptimizationPriorityId,
                        EndUserNotificationId = group.EndUserNotificationId,
                        FilterId = group.FilterId,
                        FilterModeId = group.FilterModeId,
                        GroupModeId = group.GroupModeId
                    };

                    _applicationDbContext.AssignmentProfileGroups.Add(groupToBeCopied);
                }
            }

            await _applicationDbContext.SaveChangesAsync();


            foreach (AssignmentProfile? assignmentProfile in assignmentProfilesToCopy)
            {
                // Show notifications in audit log
                await _auditLogService.GenerateAuditLogAsync(
                  AuditLogCategory.AssignmentProfiles,
                  string.Format(AuditLogActions.AssignmentProfilesCopyProfile, assignmentProfile.Name),
                  author: loggedInUser,
                  saveChanges: false);

                _log.Info($"The assignment profile: '{assignmentProfile.Name}', with id: {assignmentProfile.Id} was copied.");
            }

            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of editing an assignment profile.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="newAssignmentProfileDto">The assignment profile dto to be edited</param>
        /// <returns></returns>
        public async Task<int> EditAssignmentProfileAsync(long assignmentProfileId, NewAssignmentProfileDto newAssignmentProfileDto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the assignment profile that we want to edit
            AssignmentProfile? assignmentProfileToEdit = await _applicationDbContext.AssignmentProfiles
                .Where(assignment => assignment.Id == assignmentProfileId && assignment.SubscriptionId == loggedInUser!.SubscriptionId)
                .FirstOrDefaultAsync();

            // Check if the profile is valid
            if (assignmentProfileToEdit == null)
            {
                _log.Error($"The assignment profile was null when trying to edit.");
                return StatusCodes.Status400BadRequest;
            }

            // Get the groups of the edited assignment profile and store them into a variable
            List<AssignmentProfileGroup>? editedAssignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(group => group.AssignmentProfileId == assignmentProfileId)
                .ToListAsync();

            // Delete the groups of the edited assignment profile
            _applicationDbContext.AssignmentProfileGroups.RemoveRange(editedAssignmentProfileGroups);

            await _applicationDbContext.SaveChangesAsync();

            // Set the name of the edited assignment profile
            assignmentProfileToEdit.Name = newAssignmentProfileDto.Name;
            assignmentProfileToEdit.ModifiedOn = DateTime.UtcNow;

            // Map the groups of the new edited assignment profile
            AssignmentProfileGroup[]? groups = _mapper.Map<AssignmentProfileGroup[]>(newAssignmentProfileDto.Groups);

            // Add the new groups to the edited assignment profile
            foreach (AssignmentProfileGroup? group in groups)
            {
                group.AssignmentProfileId = assignmentProfileId;
                _applicationDbContext.AssignmentProfileGroups.Add(group);
            }

            await _applicationDbContext.SaveChangesAsync();

            // Take all the private applications that have this profile assigned
            PrivateApplication[]? privateApplications = await _applicationDbContext.PrivateApplications
                .Where(application => application.AssignmentProfileId == assignmentProfileId)
                .ToArrayAsync();

            // Take all the public applications that have this profile assigned
            SubscriptionPublicApplication[]? publicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.AssignmentProfileId == assignmentProfileId)
                .ToArrayAsync();

            // For private apps
            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtosForPrivate = privateApplications
                .Select(application => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = application.SubscriptionId,
                    IntuneId = application.IntuneId,
                    IsPrivate = true,
                }).ToArray();

            // For public apps
            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtosForPublic = publicApplications
               .Select(application => new AssignmentProfileApplicationDto
               {
                   SubscriptionId = application.SubscriptionId,
                   IntuneId = application.IntuneId,
                   IsPrivate = false,
               }).ToArray();

            for (int i = 0; i < assignmentProfileApplicationDtosForPublic.Length; i++)
            {
                PublicApplication? publicApplication = await _applicationDbContext.PublicApplications
                    .FirstAsync(application => application.Id == publicApplications[i].PublicApplicationId);
                assignmentProfileApplicationDtosForPublic[i].Name = publicApplication.Name;
            }

            await AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, assignmentProfileToEdit, assignmentProfileApplicationDtosForPrivate, groups);
            await AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, assignmentProfileToEdit, assignmentProfileApplicationDtosForPublic, groups);

            string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

            // Show notifications in audit log
            await _auditLogService.GenerateAuditLogAsync(
              AuditLogCategory.AssignmentProfiles,
              string.Format(AuditLogActions.AssignmentProfilesEditProfile, assignmentProfileToEdit.Name),
              author: loggedInUser);

            _log.Info($"The assignment profile: '{assignmentProfileToEdit.Name}' with the id: {assignmentProfileToEdit.Id} was edited.");

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of assigning an assignment profile to a private application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        public async Task<int> AssignAssignmentProfileToPrivateApplicationsAsync(long assignmentProfileId, int[] applicationIds)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            return await AssignAssignmentProfileToPrivateApplicationsJobAsync(loggedInUser, loggedInUser.SubscriptionId, assignmentProfileId, applicationIds);
        }

        public async Task<int> AssignAssignmentProfileToPrivateApplicationsJobAsync(UserDto loggedInUser, Guid subscriptionId, long assignmentProfileId, int[] applicationIds)
        {
            // Check if the assignment profile already exists in the db, and save the profile in a variable
            AssignmentProfile? assignmentProfile = await _applicationDbContext.AssignmentProfiles.FindAsync(assignmentProfileId);

            // Check if the profile is valid
            if (assignmentProfile == null || assignmentProfile.SubscriptionId != subscriptionId)
            {
                _log.Error($"The assignment profile was null when trying the assign action to a private application.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the assignment profile
                await _notificationService.GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "Invalid assignment profile.");

                return StatusCodes.Status400BadRequest;
            }

            // Get the assignment profile groups of the assignment profile to be assigned
            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(group => group.AssignmentProfile.Id == assignmentProfileId)
                .ToArrayAsync();

            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            PrivateApplication[]? applications = await _applicationDbContext
                .PrivateApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => applicationIds.Contains(application.Id))
                .ToArrayAsync();

            bool containsNullIntuneId = applications.Any(application => application.IntuneId is null);

            if (containsNullIntuneId)
            {
                _log.Error("One or more intune ids of the private applications were null when trying to assign.");

                await _notificationService.GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            foreach (PrivateApplication? application in applications)
            {
                // Set the AssignmentProfileId property of the applications to the id of the assignment profile we want to assign
                application.AssignmentProfileId = assignmentProfile.Id;
            }

            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = applications
                .Select(application => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = application.SubscriptionId,
                    IntuneId = application.IntuneId!,
                    Name = application.Name,
                    IsPrivate = true,
                })
                .ToArray();

            await AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, assignmentProfile, assignmentProfileApplicationDtos, assignmentProfileGroups);

            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of assigning an assignment profile to a public application.
        /// </summary>
        /// <param name="assignmentProfileId">The id of the assignment profile that will be assigned</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        public async Task<int> AssignAssignmentProfileToPublicApplicationsAsync(long assignmentProfileId, int[] applicationIds)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            return await AssignAssignmentProfileToPublicApplicationsJobAsync(loggedInUser, loggedInUser.SubscriptionId, assignmentProfileId, applicationIds);
        }

        public async Task<int> AssignAssignmentProfileToPublicApplicationsJobAsync(UserDto loggedInUser, Guid subscriptionId, long assignmentProfileId, int[] applicationIds)
        {
            // Check if the assignment profile already exists in the db, and save the profile in a variable
            AssignmentProfile? assignmentProfile = await _applicationDbContext.AssignmentProfiles.FindAsync(assignmentProfileId);

            // Check if the profile is valid
            if (assignmentProfile == null || assignmentProfile.SubscriptionId != subscriptionId)
            {
                _log.Error($"The assignment profile was invalid when trying the assign action to a public application.");

                // Show notifications for failed clearing the assignment profile
                await _notificationService.GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
                    subscriptionId,
                    loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "Invalid assignment profile.");

                return StatusCodes.Status400BadRequest;
            }

            // Get the assignment profile groups of the assignment profile to be assigned
            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(group => group.AssignmentProfile.Id == assignmentProfileId)
                .ToArrayAsync();

            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            SubscriptionPublicApplication[]? subscriptionPublicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(subscriptionPublicApplication => applicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .Include(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplication)
                .ToArrayAsync();

            bool containsNullIntuneId = subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.IntuneId is null);
            bool containsUndeployedApplication = applicationIds.Any(applicationId => !subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplicationId == applicationId));

            if (containsNullIntuneId || containsUndeployedApplication)
            {
                _log.Error("One or more intune ids of the public applications were null when trying to assign.");

                // Show notifications for failed clearing the assignment profile
                await _notificationService.GenerateFailedAssignmentProfileAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                // Set the AssignmentProfileId property of the applications to the id of the assignment profile we want to assign
                subscriptionPublicApplication.AssignmentProfileId = assignmentProfile.Id;
            }

            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = subscriptionPublicApplications
                .Select(subscriptionPublicApplication => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = subscriptionPublicApplication.SubscriptionId,
                    IntuneId = subscriptionPublicApplication.IntuneId,
                    Name = subscriptionPublicApplication.PublicApplication.Name,
                    IsPrivate = false,
                })
                .ToArray();

            await AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, assignmentProfile, assignmentProfileApplicationDtos, assignmentProfileGroups);
            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of clearing the assignment profile from the private applications.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> ClearAssignedProfilesFromPrivateApplicationsAsync(int[] applicationIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            List<PrivateApplication>? applications = await _applicationDbContext.PrivateApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => applicationIds.Contains(application.Id))
                .ToListAsync();

            bool containsNullIntuneId = applications.Any(application => application.IntuneId is null);

            if (containsNullIntuneId)
            {
                _log.Error("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the assignment profile
                await _notificationService.GenerateFailedClearAssignmentProfileNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            foreach (PrivateApplication? application in applications)
            {
                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Set the AssignmentProfileId property of the applications to null, meaning they no longer have no assignment profile
                application.AssignmentProfileId = null;
            }

            // Take all the intune ids and select only the not null ones
            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = applications
                .Where(application => application.IntuneId is not null)
                .Select(application => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = application.SubscriptionId,
                    IntuneId = application.IntuneId!,
                    Name = application.Name,
                    IsPrivate = true
                })
                .ToArray();

            ClearAssignedProfiles(loggedInUser, assignmentProfileApplicationDtos);
            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Handles the action of clearing the assignment profiles from public applications.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="applicationIds">The ids of the applications that will have an assignment profile assigned</param>
        /// <returns></returns>
        public async Task<int> ClearAssignedProfilesFromPublicApplicationsAsync(int[] applicationIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            SubscriptionPublicApplication[]? subscriptionPublicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(subscriptionPublicApplication => applicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .Include(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplication)
                .ToArrayAsync();

            bool containsNullIntuneId = subscriptionPublicApplications.Any(application => application.IntuneId is null);
            bool containsUndeployedApplication = applicationIds.Any(applicationId => !subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplicationId == applicationId));

            if (containsNullIntuneId || containsUndeployedApplication)
            {
                _log.Error("One or more intune ids of the public applications were null when trying to clear.");

                // Show notifications for failed clearing the assignment profile
                await _notificationService.GenerateFailedClearAssignmentProfileNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                // Set the AssignmentProfileId property of the applications to null, meaning they no longer have no assignment profile
                subscriptionPublicApplication.AssignmentProfileId = null;
            }

            // Take all the intune ids and select only the not null ones
            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = subscriptionPublicApplications
               .Where(subscriptionPublicApplication => subscriptionPublicApplication.IntuneId is not null)
               .Select(subscriptionPublicApplication => new AssignmentProfileApplicationDto
               {
                   SubscriptionId = subscriptionPublicApplication.SubscriptionId,
                   IntuneId = subscriptionPublicApplication.IntuneId,
                   Name = subscriptionPublicApplication.PublicApplication.Name,
                   IsPrivate = false,
               })
               .ToArray();

            ClearAssignedProfiles(loggedInUser, assignmentProfileApplicationDtos);
            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Clears the assignment profile from an application in EndpointAdmin
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="application">The application we want to clear an assignment profile from</param>
        /// <returns>Void</returns>
        public async Task ClearAssignedProfileAsync(UserDto loggedInUser, AssignmentProfileApplicationDto application)
        {
            AssignmentProfileGroupDto[]? empty = Array.Empty<AssignmentProfileGroupDto>();
            try
            {
                await _graphAssignmentService.SetMobileAppAssignmentAsync(application, empty);

                await _notificationService.GenerateSuccessfulClearAssignmentProfileNotificationsAsync(
                    application.Name,
                    loggedInUser.SubscriptionId,
                    loggedInUser.Id,
                    application.IsPrivate);
                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.AssignmentProfiles, 
                    string.Format(AuditLogActions.AssignmentProfilesClearProfile, application.Name),
                    author: loggedInUser);
                _log.Info($"Clear assignment profile from application: '{application.Name}' with id: {application.IntuneId}.");
            }
            catch
            {
                await _notificationService.GenerateFailedAssignmentProfileClearApplyNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    application.Name,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: application.IsPrivate
                    );
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="assignmentProfile">The assignment profile we want to assign to applications</param>
        /// <param name="application">The application we want to assign an assignment profile to</param>
        /// <param name="assignmentProfileGroupDtos">The azure groups of the assignment profile we want to assign</param>
        /// <returns>Void</returns>
        public async Task AssignAssignmentProfileToApplicationNoCheckAsync(UserDto loggedInUser, AssignmentProfileDto assignmentProfile, AssignmentProfileApplicationDto application, AssignmentProfileGroupDto[] assignmentProfileGroupDtos, bool generateNotifications)
        {
            try
            {
                await _graphAssignmentService.SetMobileAppAssignmentAsync(application, assignmentProfileGroupDtos);

                if (generateNotifications)
                {
                    //// Show notifications for assigning the assignment profile
                    await _notificationService.GenerateSuccessfulAssignmentProfileAssignmentNotificationsAsync(
                        assignmentProfile.Name,
                        application.Name,
                        loggedInUser.SubscriptionId,
                        loggedInUser.Id,
                        isForPrivateRepository: application.IsPrivate);
                }

                //// Show notifications in audit log
                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.AssignmentProfiles,
                    string.Format(AuditLogActions.AssignmentProfilesAssignProfile, assignmentProfile.Name, application.Name), 
                    author: loggedInUser);

                _log.Info($"Assignment profile '{assignmentProfile.Name}' with the id: {assignmentProfile.Id} was assigned to the application: {application.Name}.");
            }
            catch
            {
                await _notificationService.GenerateFailedAssignmentProfileAssignApplyNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    assignmentProfile.Name,
                    application.Name,
                    loggedInUser.Id);
            }
        }

        /// <summary>
        /// Handles the assignment action to private application without checking for assignment profile existence.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="assignmentProfile">The assignment profile we want to assign to applications</param>
        /// <param name="applications">The application we want to assign an assignment profile to</param>
        /// <param name="assignmentProfileGroups">The azure groups of the assignment profile we want to assign</param>
        /// <returns>Void</returns>
        public async Task AssignAssignmentProfileToApplicationsNoCheckAsync(UserDto loggedInUser, AssignmentProfile assignmentProfile, AssignmentProfileApplicationDto[] applications, AssignmentProfileGroup[] assignmentProfileGroups, bool generateNotifications = true)
        {
            // Map AssignmentProfileGroup to AssignmentProfileGroupDto
            AssignmentProfileGroupDto[]? assignmentProfileGroupDtos = _mapper.Map<AssignmentProfileGroupDto[]>(assignmentProfileGroups);
            AssignmentProfileDto? assignmentProfileDto = _mapper.Map<AssignmentProfileDto>(assignmentProfile);

            foreach (AssignmentProfileApplicationDto? application in applications)
            {
                // Assign the groups in intune
                _backgroundJobService.Enqueue(() => AssignAssignmentProfileToApplicationNoCheckAsync(loggedInUser, assignmentProfileDto, application, assignmentProfileGroupDtos, generateNotifications));
            }
        }

        /// <summary>
        /// Clears the assignment profiles from intune.
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="applications">The application we want to clear an assignment profile from</param>
        public void ClearAssignedProfiles(UserDto loggedInUser, AssignmentProfileApplicationDto[] applications)
        {
            foreach (AssignmentProfileApplicationDto? application in applications)
            {
                _backgroundJobService.Enqueue(() => ClearAssignedProfileAsync(loggedInUser, application));
            }
        }
    }
}