using AutoMapper;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Results;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class PrivateApplicationService : IPrivateApplicationService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly IDeploymentScheduleJobService _deploymentScheduleJobService;

        public PrivateApplicationService(
           IApplicationDbContext applicationDbContext,
           IAzureBlobService azureBlobService,
           IMapper mapper,
           INotificationService notificationService,
           IAuditLogService auditLogService,
           IBackgroundJobService backgroundJobService,
           IDeployIntunewinService deployIntunewinService,
           ILoggedInUserProvider loggedInUserProvider,
           IDeploymentScheduleJobService deploymentScheduleJobService)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _azureBlobService = azureBlobService;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _backgroundJobService = backgroundJobService;
            _deployIntunewinService = deployIntunewinService;
            _loggedInUserProvider = loggedInUserProvider;
            _deploymentScheduleJobService = deploymentScheduleJobService;
        }

        /// <summary>
        /// Creates a list with the ids of private applications
        /// </summary>
        /// <returns>An enumerable containing the ids of the private applications</returns>
        public async Task<IEnumerable<int>> ListPrivateApplicationsIdsAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            return await _applicationDbContext
                .PrivateApplications
                .Where(pa => pa.SubscriptionId == subscriptionId)
                .Select(pa => pa.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Lists all private applications and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which than pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific private application by name</param>
        /// <returns>A paged result with all private applications paged</returns>
        public async Task<PagedResult<PrivateApplicationDto>> ListPrivateApplicationsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            IQueryable<PrivateApplication>? queryPrivateApplications = _applicationDbContext
                .PrivateApplications
                .Where(pa => pa.SubscriptionId == subscriptionId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryPrivateApplications = queryPrivateApplications.Where(pa => pa.Name.Contains(searchTerm.Trim()));
            }

            List<PrivateApplication>? privateApplications = await queryPrivateApplications
                    .OrderByDescending(privateApp => !string.IsNullOrEmpty(privateApp.DeploymentStatus))
                    .ThenBy(privateApp => privateApp.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(privateApp => privateApp.AssignmentProfile)
                    .Include(privateApp => privateApp.DeploymentSchedule)
                    .ToListAsync();

            List<PrivateApplicationDto>? privateApplicationDtos = _mapper.Map<List<PrivateApplicationDto>>(privateApplications);

            for (int i = 0; i < privateApplications.Count; i++)
            {
                var privateApplication = privateApplications[i];
                var privateApplicationDto = privateApplicationDtos[i];

                privateApplicationDto.AssignedProfileName = privateApplication.AssignmentProfile?.Name;
                privateApplicationDto.AssignedDeploymentSchedule = _mapper.Map<DeploymentScheduleDto>(privateApplication.DeploymentSchedule);

                var deploymentScheduleApplication = privateApplication
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .FirstOrDefault();

                if (deploymentScheduleApplication is not null)
                {
                    privateApplicationDto.AssignedDeploymentScheduleInProgress = true;
                    privateApplicationDto.AssignedDeploymentSchedulePhaseState = deploymentScheduleApplication.PhaseState;
                    privateApplicationDto.AssignedDeploymentSchedulePhaseName = deploymentScheduleApplication.CurrentPhase?.Name;
                }
            }

            return new PagedResult<PrivateApplicationDto>
            {
                AllItemsCount = await queryPrivateApplications.CountAsync(),
                PageItems = privateApplicationDtos
            };
        }

        /// <summary>
        /// Handles the action of adding or updating a private application
        /// </summary>
        /// <param name="privateApplicationDto">The private application information that is about to be added or changed</param>
        /// <returns>The id of the application that was added or changed</returns>
        public async Task<int> AddOrUpdatePrivateApplicationAsync(PrivateApplicationDto privateApplicationDto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            string? archiveFileName = await UploadArchiveToPrivateRepository(loggedInUser.SubscriptionId, privateApplicationDto);

            bool isNewApplication = true;
            bool isNewVersion = false;

            PrivateApplication? privateApplication = await _applicationDbContext
                .PrivateApplications
                .SingleOrDefaultAsync(pa =>
                    pa.Name == privateApplicationDto.Name &&
                    pa.Architecture == privateApplicationDto.Architecture &&
                    pa.SubscriptionId == loggedInUser.SubscriptionId
                );

            if (privateApplication != null)
            {
                isNewApplication = false;

                int versionCheck = ApplicationHelper.CompareAppVersions(
                    privateApplicationDto.Version,
                    privateApplication.DeployedVersion ?? privateApplication.Version
                );

                if (versionCheck >= 0 && privateApplication.DeploymentStatus == DeploymentStatus.SuccessfulUpToDate)
                {
                    privateApplication.DeploymentStatus = DeploymentStatus.SuccessfulNotUpToDate;
                }

                if (versionCheck > 0)
                {
                    isNewVersion = true;
                }

                privateApplication = _mapper.Map(privateApplicationDto, privateApplication);
                privateApplication.PrivateRepositoryArchiveFileName = archiveFileName;
            }
            else
            {
                PrivateApplication? newPrivateApplication = _mapper.Map<PrivateApplication>(privateApplicationDto);
                newPrivateApplication.PrivateRepositoryArchiveFileName = archiveFileName;
                newPrivateApplication.SubscriptionId = loggedInUser.SubscriptionId;

                privateApplication = _applicationDbContext.PrivateApplications.Add(newPrivateApplication).Entity;
            }

            await _applicationDbContext.SaveChangesAsync();

            string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

            if (isNewApplication)
            {
                await _notificationService.GenerateNewApplicationNotificationsAsync(
                    privateApplication,
                    loggedInUser.SubscriptionId,
                    true,
                    authorId: superAdminId);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PrivateRepository,
                    string.Format(AuditLogActions.RepositoryAddApp, privateApplicationDto.Name, RepositoryType.Private.ToLower()),
                    author: loggedInUser);

                _log.Info($"The private application: '{privateApplication.Name}' with the id: {privateApplication.Id} was added.");
            }

            if (isNewVersion)
            {                
                if (privateApplication.DeploymentScheduleId != null)
                {
                    var deploymentSchedule = await _applicationDbContext
                        .DeploymentSchedules
                        .Where(ds => ds.Id == privateApplication.DeploymentScheduleId)
                        .FirstOrDefaultAsync();

                    var inProgress = privateApplication
                        .DeploymentScheduleApplications
                        .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                        .Any();

                    if (deploymentSchedule.CronTrigger is null && !inProgress)
                    {
                        var application = (privateApplication.Id, true);
                        _backgroundJobService.Enqueue(
                            () => TriggerDeploymentSchedule(loggedInUser, deploymentSchedule.Id, application)
                            );
                    }
                }
            }

            return privateApplication.Id;
        }

        public async Task TriggerDeploymentSchedule(UserDto loggedInUser, long deploymentScheduleId, (int Id, bool IsPrivate) application)
        {
            await _deploymentScheduleJobService.TriggerJobAsync(loggedInUser,loggedInUser.SubscriptionId, deploymentScheduleId, application);
        }

        /// <summary>
        /// Uploads the zip file of private applications in private repository
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="privateApplicationDto">The private application information that is uploaded on private repository</param>
        /// <returns>The name of the final file archive</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> UploadArchiveToPrivateRepository(Guid subscriptionId, PrivateApplicationDto privateApplicationDto)
        {
            string? mountPath = ApplicationHelper.GetMountPath();
            string? tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(privateApplicationDto, subscriptionId);

            string? tempArchiveFullPath = Path.Combine(mountPath, tempArchiveFileName);
            if (!File.Exists(tempArchiveFullPath))
            {
                _log.Error($"Archive not found at the expected path: {tempArchiveFullPath}.");
                throw new Exception($"Archive not found at the expected path: {tempArchiveFullPath}");
            }


            string? finalArchiveFileName = ApplicationHelper.GetArchiveFileNameFromInfo(privateApplicationDto, subscriptionId);
            string? finalArchiveFullPath = Path.Combine(mountPath, finalArchiveFileName);

            File.Move(tempArchiveFullPath, finalArchiveFullPath, true);

            try
            {
                await _azureBlobService.UploadPackageOnPrivateRepositoryAsync(subscriptionId, finalArchiveFullPath);
            }
            finally
            {
                File.Delete(finalArchiveFullPath);
            }

            return finalArchiveFileName;
        }

        /// <summary>
        /// Handles the action of removing a private application
        /// </summary>
        /// <param name="applicationIds">The ids of the private applications we want to remove</param>
        /// <returns>A status representing the state of the action</returns>
        public async Task<int> RemovePrivateApplicationsAsync(int[] applicationIds)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;

            PrivateApplication[]? privateApplications = await _applicationDbContext
                .PrivateApplications
                .Include(a => a.DeploymentScheduleApplications)
                .Where(x => applicationIds.Contains(x.Id)).ToArrayAsync();

            if (privateApplications.Length == 0)
            {
                _log.Error($"The private applications with the ids {string.Join(',', applicationIds)} does not exists.");
                return StatusCodes.Status400BadRequest;
            }

            foreach (PrivateApplication privateApplication in privateApplications)
            {
                if (privateApplication.SubscriptionId != subscriptionId)
                {
                    return StatusCodes.Status401Unauthorized;
                }
            }

            foreach (PrivateApplication privateApp in privateApplications)
            {
                await _notificationService.GenerateDeleteApplicationNotificationsAsync(privateApp, subscriptionId, true, authorId: loggedInUser.Id);

                _applicationDbContext.PrivateApplications.Remove(privateApp);

                await _auditLogService.GenerateAuditLogAsync(
               AuditLogCategory.PrivateRepository,
               string.Format(AuditLogActions.RepositoryRemoveApp, privateApp.Name, RepositoryType.Private.ToLower()),
               author: loggedInUser,
               saveChanges: false);

                _log.Info($"The private application: '{privateApp.Name}' with the id: {privateApp.Id} was removed.");
            }

            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// Gets the download link for an application from private repository
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get the download link</param>
        /// <returns>The key value pair with the download link and a status code</returns>
        public async Task<(Uri, int)> GetDownloadUriForPrivateApplicationAsync(int applicationId)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            PrivateApplication? application = await _applicationDbContext
                .PrivateApplications
                .SingleAsync(app => app.Id == applicationId);

            if (application.SubscriptionId != subscriptionId)
            {
                _log.Error($"The private application '{application.Name}' with the id: {application.Id} is not in the current subscription.");
                return (null, StatusCodes.Status401Unauthorized);
            }

            Uri? uri = _azureBlobService
                .GetDownloadUriForPackageUsingSas(application.PrivateRepositoryArchiveFileName, subscriptionId);

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.PrivateRepository,
                string.Format(AuditLogActions.RepositoryDownloadApp, application.Name, RepositoryType.Private.ToLower()),
                author: loggedInUser);

            _log.Info($"The user downloaded the private application '{application.Name}' with the id: {application.Id}.");

            return (uri, StatusCodes.Status200OK);
        }

        /// <summary>
        /// Starts the deployment of the private applications
        /// </summary>
        /// <param name="applicationIds">The ids of the private applications we want to deploy</param>
        /// <returns>A status code representing the state of the action</returns>
        public async Task<Result> StartDeployAsync(IEnumerable<int> applicationIds)
        {
            var ids = applicationIds.ToArray();

            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            List<PrivateApplication>? privateApplications = await _applicationDbContext
                .PrivateApplications
                .Where(app => ids.Contains(app.Id))
                .Where(app => app.SubscriptionId == loggedInUser.SubscriptionId)
                .ToListAsync();

            if (privateApplications.Count != ids.Length)
            {
                _log.Error($"One or more applications are not in the current subscription.");
                return new NotFoundResult("One or more applications are not in the current subscription.");
            }

            if (privateApplications.Any(app => app.DeploymentStatus == DeploymentStatus.InProgress))
            {
                _log.Error($"One or more applications are already deploying.");
                return new InvalidOperationResult("One or more applications are already deploying.");
            }

            if (privateApplications.Any(app => app.DeploymentScheduleApplications.Any(a => a.Type == DeploymentScheduleApplicationType.Current)))
            {
                _log.Error($"One or more applications are part of a deployment schedule in progress.");
                return new InvalidOperationResult("One or more applications are part of a deployment schedule in progress.");
            }

            foreach (PrivateApplication? privateApp in privateApplications)
            {
                privateApp.DeploymentStatus = DeploymentStatus.InProgress;
            }

            await _applicationDbContext.SaveChangesAsync();

            foreach (PrivateApplication? privateApp in privateApplications)
            {
                _backgroundJobService.Enqueue(() =>
                    _deployIntunewinService.DeployPrivateApplicationForSubscriptionAsync(loggedInUser, privateApp.Id, null, false)
                );

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PrivateRepository,
                    string.Format(AuditLogActions.RepositoryDeployApp, privateApp.Name, RepositoryType.Private.ToLower()),
                    author: loggedInUser,
                    saveChanges: false);

                _log.Info($"The user started the deploy of the application '{privateApp.Name}' with the id: {privateApp.Id}");
            }

            await _applicationDbContext.SaveChangesAsync();

            return new SuccessResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<Response<ApplicationDto>> UploadAsync(IFormFile file)
        {
            string? mountPath = ApplicationHelper.GetMountPath();

            string archiveFullPath = Path.Combine(mountPath, Guid.NewGuid() + ".zip");

            using (FileStream stream = File.Create(archiveFullPath))
            {
                await file.CopyToAsync(stream);
            }

            Response<ApplicationDto> response = await ApplicationHelper.ValidateAndGetInfo(archiveFullPath, true);

            if (!response.IsSuccessful)
            {
                return response;
            }

            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            string tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(response.Dto, loggedInUser.SubscriptionId);
            File.Move(archiveFullPath, Path.Combine(mountPath, tempArchiveFileName), true);

            PrivateApplication? existingPrivateApplication = await _applicationDbContext
                .PrivateApplications
                .SingleOrDefaultAsync(pa =>
                    pa.Name == response.Dto.Name &&
                    pa.Architecture == response.Dto.Architecture &&
                    pa.SubscriptionId == loggedInUser.SubscriptionId
                );

            if (existingPrivateApplication is null)
            {
                return response;
            }

            response.Dto.ExistingVersion = existingPrivateApplication.Version;

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<int[]> CopyApplicationsAsync(int[] applicationIds)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            return await CopyApplicationsJobAsync(loggedInUser, applicationIds);
        }

        public async Task<int[]> CopyApplicationsJobAsync(UserDto loggedInUser, int[] applicationIds)
        {
            // Search for the application that has the id given as parameter
            PrivateApplication[] privateApplications = await _applicationDbContext
                .PrivateApplications
                .Where(pa => applicationIds.Contains(pa.Id) &&
                    pa.SubscriptionId == loggedInUser.SubscriptionId
                )
                .ToArrayAsync();

            // Check if the application is null
            if (privateApplications.Length != applicationIds.Length)
            {
                throw new InvalidOperationException();
            }

            List<PrivateApplication> copiedPrivateApplications = new List<PrivateApplication>(privateApplications.Length);

            foreach (PrivateApplication application in privateApplications)
            {
                PrivateApplication privateApplicationCopy = new PrivateApplication
                {
                    Name = application.Name + " - Copy",
                    Architecture = application.Architecture,
                    SubscriptionId = application.SubscriptionId,
                    RunAs32Bit = application.RunAs32Bit,
                    Description = application.Description,
                    IconBase64 = application.IconBase64,
                    InformationUrl = application.InformationUrl,
                    Language = application.Language,
                    Notes = application.Notes,
                    Publisher = application.Publisher,
                    Version = application.Version,
                    PrivateRepositoryArchiveFileName = application.PrivateRepositoryArchiveFileName,
                };

                copiedPrivateApplications.Add(privateApplicationCopy);

                _applicationDbContext.PrivateApplications.Add(privateApplicationCopy);

            }

            await _applicationDbContext.SaveChangesAsync();

            return copiedPrivateApplications
                .Select(application => application.Id)
                .ToArray();
        }
    }
}
