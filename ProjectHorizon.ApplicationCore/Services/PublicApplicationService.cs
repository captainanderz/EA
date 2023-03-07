using AutoMapper;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Results;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class PublicApplicationService : IPublicApplicationService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IIntuneConverterService _intuneConverterService;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<SignalRHub> _messageHubContext;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly IDeploymentScheduleJobService _deploymentScheduleJobService;

        public PublicApplicationService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            IAzureBlobService azureBlobService,
            IIntuneConverterService intuneConverterService,
            IDeployIntunewinService deployIntunewinService,
            INotificationService notificationService,
            IHubContext<SignalRHub> messageHubContext,
            IBackgroundJobService backgroundJobService,
            IAuditLogService auditLogService,
            ILoggedInUserProvider loggedInUserProvider, 
            IDeploymentScheduleJobService deploymentScheduleJobService)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _azureBlobService = azureBlobService;
            _intuneConverterService = intuneConverterService;
            _deployIntunewinService = deployIntunewinService;
            _notificationService = notificationService;
            _messageHubContext = messageHubContext;
            _backgroundJobService = backgroundJobService;
            _auditLogService = auditLogService;
            _loggedInUserProvider = loggedInUserProvider;
            _deploymentScheduleJobService = deploymentScheduleJobService;
        }

        /// <summary>
        /// Lists all public applications and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific public application by name</param>
        /// <returns>A paged result with all public applications paged</returns>
        public async Task<PagedResult<PublicApplicationDto>> ListPublicApplicationsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            IQueryable<PublicApplication>? queryPublicApplications = _applicationDbContext.PublicApplications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryPublicApplications = queryPublicApplications.Where(pa => pa.Name.Contains(searchTerm.Trim()));
            }

            PagedResult<PublicApplicationDto>? pagedResult = new PagedResult<PublicApplicationDto>
            {
                AllItemsCount = await queryPublicApplications.CountAsync()
            };

            var pubAppsOnPage = await queryPublicApplications
                .GroupJoin(
                    _applicationDbContext.SubscriptionPublicApplications,
                    pa => new { PublicApplicationId = pa.Id, SubscriptionId = subscriptionId },
                    spa => new { spa.PublicApplicationId, spa.SubscriptionId },
                    (pa, spas) => new
                    {
                        PublicApplication = pa,
                        SubscriptionPublicApplications = spas
                    }) //each entry will have one PublicApplication and a list of SubscriptionPublicApplication
                       //filtered by subscriptionId given and publicApplicationId
                .SelectMany(
                    group => group.SubscriptionPublicApplications.DefaultIfEmpty(),
                    (group, spa) => new
                    {
                        group.PublicApplication,
                        SubscriptionPublicApplication = spa
                    })//SelectMany takes each entry(one PublicApplication, list of SubscriptionPublicApplication)
                      //and creates pairs of one PublicApplication and one SubscriptionPublicApplication
                      //each pair is formed by combining each SubscriptionPublicApplication from the list of the entry
                      //with the PublicApplication of the entry
                .OrderByDescending(group => group.SubscriptionPublicApplication.DeploymentStatus != null)
                .ThenBy(group => group.PublicApplication.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            PublicApplicationDto[]? publicApplicationDtos = new PublicApplicationDto[pubAppsOnPage.Count];

            for (int i = 0; i < pubAppsOnPage.Count; i++)
            {
                var group = pubAppsOnPage[i];
                PublicApplicationDto? pubAppDto = _mapper.Map<PublicApplicationDto>(group.PublicApplication);

                if (group.SubscriptionPublicApplication != null)
                {
                    pubAppDto.AutoUpdate = group.SubscriptionPublicApplication.AutoUpdate;
                    pubAppDto.ManualApprove = group.SubscriptionPublicApplication.ManualApprove;
                    pubAppDto.DeploymentStatus = group.SubscriptionPublicApplication.DeploymentStatus;

                    pubAppDto.AssignedProfileName = group.SubscriptionPublicApplication.AssignmentProfile?.Name;
                    pubAppDto.AssignedDeploymentSchedule = _mapper.Map<DeploymentScheduleDto>(group.SubscriptionPublicApplication.DeploymentSchedule);
                    pubAppDto.IsInShop = group.SubscriptionPublicApplication.IsInShop;

                    var deploymentScheduleApplication = group
                        .SubscriptionPublicApplication
                        .DeploymentScheduleApplications
                        .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                        .FirstOrDefault();

                    if (deploymentScheduleApplication is not null)
                    {
                        pubAppDto.AssignedDeploymentScheduleInProgress = true;
                        pubAppDto.AssignedDeploymentSchedulePhaseState = deploymentScheduleApplication.PhaseState;
                        pubAppDto.AssignedDeploymentSchedulePhaseName = deploymentScheduleApplication.CurrentPhase?.Name;
                    }
                }

                publicApplicationDtos[i] = pubAppDto;
            }

            pagedResult.PageItems = publicApplicationDtos;

            return pagedResult;
        }

        /// <summary>
        /// Creates a list with the ids of public applications
        /// </summary>
        /// <returns>An enumerable containing the ids of the public applications</returns>
        public async Task<IEnumerable<int>> ListPublicApplicationsIdsAsync() =>
            await _applicationDbContext
                .PublicApplications
                .Select(pa => pa.Id)
                .ToListAsync();

        /// <summary>
        /// Handles the action of adding or updating a public application
        /// </summary>
        /// <param name="publicApplicationDto">The public application information that is about to be added or changed</param>
        /// <returns>The id of the application that was added or changed</returns>
        public async Task<int> AddOrUpdatePublicApplicationAsync(PublicApplicationDto publicApplicationDto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            string archiveFileName = await UploadArchiveToPublicRepository(publicApplicationDto);
            string packageFolderName = null;
            bool conversionFailed = false;

            try
            {
                packageFolderName = await _intuneConverterService.ConvertToIntuneAndUpload(archiveFileName, publicApplicationDto.IconFileName);
            }
            catch
            {
                _log.Error($"The conversion to intune failed");
                conversionFailed = true;
            }

            (PublicApplication publicApplication, bool isNewApplication, bool isNewVersion) =
                await AddOrUpdateApplicationInDatabase(publicApplicationDto, archiveFileName, packageFolderName);

            await _applicationDbContext.SaveChangesAsync();

            if (isNewApplication)
            {
                await _notificationService.GenerateNewApplicationNotificationsAsync(
                   publicApplication,
                   loggedInUser.SubscriptionId,
                   false,
                   authorId: loggedInUser.Id);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PublicRepository,
                    string.Format(AuditLogActions.RepositoryAddApp, publicApplication.Name, RepositoryType.Public.ToLower()),
                    author: loggedInUser);

                _log.Info($"The user added the public application: '{publicApplication.Name}' with the id: {publicApplication.Id}.");

                if (conversionFailed)
                {
                    await _notificationService.GenerateNewApplicationWarningNotificationsAsync(
                        loggedInUser.SubscriptionId,
                        publicApplication.Name,
                        authorId: loggedInUser.Id);
                }
            }

            if (isNewVersion)
            {
                await _notificationService.GenerateNewVersionNotificationsAsync(
                   publicApplication,
                   loggedInUser.SubscriptionId,
                   authorId: loggedInUser.Id);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PublicRepository,
                    string.Format(AuditLogActions.RepositoryNewVersionApp, publicApplication.Name, RepositoryType.Public.ToLower()),
                    author: loggedInUser);

                _log.Info($"The user updated the public application: '{publicApplication.Name}' with the id: {publicApplication.Id} to a new version.");

                if (conversionFailed)
                {
                    await _notificationService.GenerateNewApplicationWarningNotificationsAsync(
                        loggedInUser.SubscriptionId,
                        publicApplication.Name,
                        authorId: loggedInUser.Id);
                }
            }

            return publicApplication.Id;
        }

        /// <summary>
        /// Gets the download link for an application from public repository
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get the download link</param>
        /// <returns>The download link</returns>
        public async Task<Uri?> GetDownloadUriForPublicApplicationAsync(int applicationId)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            var subscription = await _applicationDbContext.Subscriptions.FindAsync(loggedInUser.SubscriptionId);

            if (subscription?.GraphConfigId is null)
            {
                return null;
            }

            PublicApplication? publicApplication = await _applicationDbContext
                .PublicApplications
                .SingleAsync(app => app.Id == applicationId);

            Uri? uri = _azureBlobService
                .GetDownloadUriForPackageUsingSas(publicApplication.PublicRepositoryArchiveFileName);

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.PublicRepository,
                string.Format(AuditLogActions.RepositoryDownloadApp, publicApplication.Name, RepositoryType.Public.ToLower()),
                author: loggedInUser);

            _log.Info($"The user downloaded the public application '{publicApplication.Name}' with the id: {publicApplication.Id}.");

            return uri;
        }

        /// <summary>
        /// Handles the auto-update action of a public application
        /// </summary>
        /// <param name="applicationId">The id of the application that has the auto-update option enabled</param>
        /// <param name="autoUpdate">A bool that determines if the auto-update option is enabled or not</param>
        /// <returns>A bool indicating if the public application has the auto-update option enabled or not</returns>
        public async Task<bool> UpdateSubscriptionPublicApplicationAutoUpdateAsync(
            int applicationId,
            bool autoUpdate)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            SubscriptionPublicApplication? subPubApp = await _applicationDbContext
                .SubscriptionPublicApplications
                .Include(a => a.DeploymentSchedule)
                .Include(a => a.PublicApplication)
                .SingleAsync(subPubApp =>
                             subPubApp.PublicApplicationId == applicationId &&
                             subPubApp.SubscriptionId == subscriptionId);

            subPubApp.AutoUpdate = autoUpdate;


            if (subPubApp.AutoUpdate)
            {
                subPubApp.ManualApprove = false;
            }

            await _applicationDbContext.SaveChangesAsync();
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            if (autoUpdate && subPubApp.DeploymentStatus != DeploymentStatus.SuccessfulUpToDate && subPubApp.DeploymentStatus != DeploymentStatus.InProgress)
            {
                if (subPubApp.DeploymentSchedule is null)
                {
                    await DeployApplicationAsync(loggedInUser, subPubApp.PublicApplication);
                }
                else
                {
                    var deploymentSchedule = subPubApp.DeploymentSchedule;
                    var inProgress = subPubApp
                        .DeploymentScheduleApplications
                        .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                        .Any();

                    if (deploymentSchedule.CronTrigger is null && !inProgress)
                    {
                        var application = (subPubApp.PublicApplicationId, false);
                        _backgroundJobService.Enqueue(
                            () => TriggerDeploymentSchedule(loggedInUser, subPubApp.SubscriptionId, deploymentSchedule.Id, application)
                            );
                    }
                }
            }

            return autoUpdate;
        }

        /// <summary>
        /// Handles the manual-approve action of a public application
        /// </summary>
        /// <param name="applicationId">The id of the application that has the manual-approve option enabled</param>
        /// <param name="manualApprove">A bool that determines if the manual-approve option is enabled or not</param>
        /// <returns>A bool indicating if the public application has the manual-approve option enabled or not</returns>
        public async Task<bool> UpdateSubscriptionPublicApplicationManualApproveAsync(
            int applicationId,
            bool manualApprove)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            SubscriptionPublicApplication? subPubApp = await _applicationDbContext
                .SubscriptionPublicApplications
                .Include(a => a.DeploymentSchedule)
                .SingleAsync(subPubApp =>
                             subPubApp.PublicApplicationId == applicationId &&
                             subPubApp.SubscriptionId == subscriptionId);

            subPubApp.ManualApprove = manualApprove;

            if (subPubApp.DeploymentSchedule is not null)
            {
                subPubApp.ManualApprove = false;
            }

            if (subPubApp.ManualApprove)
            {
                subPubApp.AutoUpdate = false;
            }

            await _applicationDbContext.SaveChangesAsync();

            return manualApprove;
        }

        /// <summary>
        /// Starts the deployment of the public applications
        /// </summary>
        /// <param name="applicationIds">The ids of the private applications we want to deploy</param>
        /// <returns>A status code representing the state of the action</returns>
        public async Task<Result> StartDeployAsync(IEnumerable<int> applicationIds)
        {
            var ids = applicationIds.ToArray();
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;

            List<PublicApplication>? publicApplications = await _applicationDbContext.PublicApplications
                .Where(pubApp => ids.Contains(pubApp.Id))
                .ToListAsync();

            var subscriptionPublicApplications = _applicationDbContext
                .SubscriptionPublicApplications
                .Where(app => applicationIds.Contains(app.PublicApplicationId))
                .Where(app => app.SubscriptionId == loggedInUser.SubscriptionId)
                .ToArray();

            if (subscriptionPublicApplications.Any(app => app.DeploymentStatus == DeploymentStatus.InProgress))
            {
                _log.Error($"One or more applications are already deploying.");
                return new InvalidOperationResult("One or more applications are already deploying.");
            }

            if (subscriptionPublicApplications.Any(app => app.DeploymentScheduleApplications.Any(a => a.Type == DeploymentScheduleApplicationType.Current)))
            {
                _log.Error($"One or more applications are part of a deployment schedule in progress.");
                return new InvalidOperationResult("One or more applications are part of a deployment schedule in progress.");
            }

            foreach (PublicApplication? pubApp in publicApplications)
            {
                await DeployApplicationAsync(loggedInUser, pubApp);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PublicRepository,
                    string.Format(AuditLogActions.RepositoryDeployApp, pubApp.Name, RepositoryType.Public.ToLower()),
                    author: loggedInUser,
                    saveChanges: false);

                _log.Info($"The users started the deployment of the public application: '{pubApp.Name}' with the id: {pubApp.Id}");
            }

            await _applicationDbContext.SaveChangesAsync();

            return new SuccessResult();
        }

        private async Task DeployApplicationAsync(UserDto loggedInUser, PublicApplication application)
        {
            var subscriptionId = loggedInUser.SubscriptionId;
            var subscriptionPublicApplication = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(x =>
                (x.PublicApplicationId == application.Id) &&
                (x.SubscriptionId == subscriptionId))
                .FirstOrDefaultAsync();

            if (subscriptionPublicApplication is null)
            {
                Subscription? subscription = await _applicationDbContext
                    .Subscriptions
                    .SingleAsync(s => s.Id == subscriptionId);

                subscriptionPublicApplication = new SubscriptionPublicApplication
                {
                    SubscriptionId = subscriptionId,
                    PublicApplicationId = application.Id,
                    DeployedVersion = application.Version,
                    DeploymentStatus = DeploymentStatus.InProgress,
                    AutoUpdate = subscription.GlobalAutoUpdate,
                    ManualApprove = subscription.GlobalManualApprove
                };

                _applicationDbContext.SubscriptionPublicApplications.Add(subscriptionPublicApplication);
            }
            else
            {
                var deploymentScheduleInProgress = subscriptionPublicApplication
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any();

                if (deploymentScheduleInProgress)
                {
                    return;
                }
            }

            subscriptionPublicApplication.DeploymentStatus = DeploymentStatus.InProgress;
            await _applicationDbContext.SaveChangesAsync();

            _backgroundJobService.Enqueue(() =>
                _deployIntunewinService.DeployPublicApplicationForSubscriptionAsync(loggedInUser, subscriptionId, application.Id, null, false)
            );
        }

        /// <summary>
        /// Uploads the zip file of public applications in public repository
        /// </summary>
        /// <param name="publicApplicationDto">The public application information that is uploaded on public repository</param>
        /// <returns>The name of the final file archive</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> UploadArchiveToPublicRepository(PublicApplicationDto publicApplicationDto)
        {
            string? mountPath = ApplicationHelper.GetMountPath();
            string? tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(publicApplicationDto);

            string? tempArchiveFullPath = Path.Combine(mountPath, tempArchiveFileName);
            if (!File.Exists(tempArchiveFullPath))
            {
                _log.Error($"Archive not found at the expected path: {tempArchiveFullPath}");
                throw new Exception($"Archive not found at the expected path: {tempArchiveFullPath}");
            }

            string? finalArchiveFileName = ApplicationHelper.GetArchiveFileNameFromInfo(publicApplicationDto);
            string? finalArchiveFullPath = Path.Combine(mountPath, finalArchiveFileName);

            File.Move(tempArchiveFullPath, finalArchiveFullPath, true);

            await _azureBlobService.UploadPublicRepositoryPackageAsync(finalArchiveFullPath);

            return finalArchiveFileName;
        }

        public async Task TriggerDeploymentSchedule(UserDto loggedInUser, Guid subscriptionId, long deploymentScheduleId, (int Id, bool IsPrivate) application)
        {
            await _deploymentScheduleJobService.TriggerJobAsync(loggedInUser, subscriptionId, deploymentScheduleId, application);
        }

        /// <summary>
        /// Handles the action of adding or updating a public application
        /// </summary>
        /// <param name="publicApplicationDto">The public application information that is about to be added or changed</param>
        /// <param name="archiveFileName">The archive file name of the public repository</param>
        /// <param name="packageFolderName">The folder name of the packages for public repository</param>
        /// <returns>A response that has 2 boolean values indicating if the public application is a new application (first bool) and if it has a new version (second bool) </returns>
        private async Task<(PublicApplication, bool, bool)> AddOrUpdateApplicationInDatabase(
            PublicApplicationDto publicApplicationDto,
            string archiveFileName,
            string packageFolderName)
        {
            PublicApplication? publicApplication = _mapper.Map<PublicApplication>(publicApplicationDto);
            publicApplication.PublicRepositoryArchiveFileName = archiveFileName;

            bool isNewApplication = true;
            bool isNewVersion = false;

            PublicApplication? existingPublicApplication = await _applicationDbContext
                .PublicApplications
                .SingleOrDefaultAsync(pa =>
                    pa.Name == publicApplicationDto.Name &&
                    pa.Architecture == publicApplicationDto.Architecture
                );

            if (existingPublicApplication != null)
            {
                isNewApplication = false;

                publicApplication.Id = existingPublicApplication.Id;
                publicApplication = _mapper.Map(publicApplication, existingPublicApplication);

                List<SubscriptionPublicApplication>? existingSubApps = await _applicationDbContext
                    .SubscriptionPublicApplications
                    .Where(x => x.PublicApplicationId == publicApplication.Id)
                    .Include(x => x.DeploymentSchedule)
                    .ToListAsync();

                UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

                foreach (SubscriptionPublicApplication? subApp in existingSubApps)
                {
                    int versionCheck = ApplicationHelper.CompareAppVersions(publicApplicationDto.Version, subApp.DeployedVersion);

                    if (versionCheck == 0 && subApp.AutoUpdate)
                    {
                        _backgroundJobService.Enqueue(() =>
                            _deployIntunewinService.DeployPublicApplicationForSubscriptionAsync(
                                loggedInUser,
                                subApp.SubscriptionId,
                                subApp.PublicApplicationId,
                                null,
                                false));
                    }
                    else if (versionCheck > 0)
                    {
                        isNewVersion = true;
                        subApp.DeploymentStatus = DeploymentStatus.SuccessfulNotUpToDate;

                        if (subApp.AutoUpdate)
                        {
                            if (subApp.DeploymentScheduleId == null)
                            {
                                _backgroundJobService.Enqueue(() =>
                                    _deployIntunewinService.DeployPublicApplicationForSubscriptionAsync(
                                        loggedInUser,
                                        subApp.SubscriptionId,
                                        subApp.PublicApplicationId,
                                        null, 
                                        false));
                            }
                            else
                            {
                                var deploymentSchedule = subApp.DeploymentSchedule;
                                var inProgress = subApp
                                    .DeploymentScheduleApplications
                                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                                    .Any();

                                if (deploymentSchedule.CronTrigger is null && !inProgress)
                                {
                                    var application = (subApp.PublicApplicationId, false);
                                    _backgroundJobService.Enqueue(
                                        () => TriggerDeploymentSchedule(loggedInUser, subApp.SubscriptionId, deploymentSchedule.Id, application)
                                        );
                                }
                            }

                            
                        }
                        else if (subApp.ManualApprove)
                        {
                            await GenerateApprovalAsync(subApp);

                            await _notificationService.GenerateManualApprovalNotificationsAsync(
                               publicApplication,
                               subApp.SubscriptionId,
                               deployedVersion: subApp.DeployedVersion);
                        }
                    }
                }

                var subscriptionUserIds = await _applicationDbContext.SubscriptionUsers
                    .Where(subscriptionUser => subscriptionUser.SubscriptionId == loggedInUser.SubscriptionId)
                    .Select(subscriptionUser => subscriptionUser.ApplicationUserId)
                    .ToArrayAsync();

                await _messageHubContext.Clients.Users(subscriptionUserIds).SendAsync(SignalRMessages.UpdateApprovalCount);
            }
            else
            {
                _applicationDbContext.PublicApplications.Add(publicApplication);
            }

            publicApplication.PackageCacheFolderName = packageFolderName;

            await _applicationDbContext.SaveChangesAsync();

            return (publicApplication, isNewApplication, isNewVersion);
        }

        /// <summary>
        /// Generates an approval base on a subscription public application
        /// </summary>
        /// <param name="subPubApp">The deployed public application we want to generate an approval for</param>
        /// <returns>Void</returns>
        private async Task GenerateApprovalAsync(SubscriptionPublicApplication subPubApp)
        {
            _applicationDbContext.Approvals
               .Where(a =>
                    a.IsActive &&
                    a.SubscriptionId == subPubApp.SubscriptionId &&
                    a.PublicApplicationId == subPubApp.PublicApplicationId)
               .ToList()
               .ForEach(a => a.IsActive = false);

            _applicationDbContext.Approvals.Add(new()
            {
                SubscriptionId = subPubApp.SubscriptionId,
                PublicApplicationId = subPubApp.PublicApplicationId,
                IsActive = true,
            });

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Handles the action of removing a public application
        /// </summary>
        /// <param name="applicationIds">The id of the public applications we want to remove</param>
        /// <returns>Void</returns>
        public async Task RemovePublicApplicationsAsync(int[] applicationIds)
        {
            PublicApplication[]? existingPublicApplications = await _applicationDbContext
                .PublicApplications
                .Where(x => applicationIds.Contains(x.Id))
                .ToArrayAsync();

            foreach(PublicApplication existingPublicApp in existingPublicApplications)
            {
                await _applicationDbContext
                   .SubscriptionPublicApplications
                   .Where(a => a.PublicApplicationId == existingPublicApp.Id)
                   .Include(a => a.DeploymentScheduleApplications)
                   .LoadAsync();
            }   

            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            var approvals = await _applicationDbContext.Approvals
                .Where(a => applicationIds.Contains(a.PublicApplicationId))
                .ToArrayAsync();

            _applicationDbContext.Approvals.RemoveRange(approvals);

            if (existingPublicApplications.Length == 0)
            {
                _log.Error($"The public applications with the ids {string.Join(',', applicationIds)} does not exists.");
            }

            foreach (PublicApplication publicApp in existingPublicApplications)
            {
                await _notificationService.GenerateDeleteApplicationNotificationsAsync(publicApp, loggedInUser.SubscriptionId, false, authorId: loggedInUser.Id);

                _applicationDbContext.PublicApplications.Remove(publicApp);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.PublicRepository,
                    string.Format(AuditLogActions.RepositoryRemoveApp, publicApp.Name, RepositoryType.Public.ToLower()),
                    saveChanges: false);

                _log.Info($"The user removed the public application: '{publicApp.Name}' with the id: {publicApp.Id}.");
            }
            await _applicationDbContext.SaveChangesAsync();
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
                _log.Info($"Response not successful; response = {JsonConvert.SerializeObject(response)}");
                return response;
            }

            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            string tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(response.Dto);
            File.Move(archiveFullPath, Path.Combine(mountPath, tempArchiveFileName), true);

            PublicApplication? existingPublicApplication = await _applicationDbContext
                .PublicApplications
                .SingleOrDefaultAsync(pa =>
                    pa.Name == response.Dto.Name &&
                    pa.Architecture == response.Dto.Architecture
                );

            if (existingPublicApplication is null)
            {
                return response;
            }

            response.Dto.ExistingVersion = existingPublicApplication.Version;

            return response;
        }
    }
}