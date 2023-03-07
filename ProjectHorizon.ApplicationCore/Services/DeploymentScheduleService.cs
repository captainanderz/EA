using AutoMapper;
using AutoMapper.QueryableExtensions;
using Hangfire;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Results;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class DeploymentScheduleService : IDeploymentScheduleService, IDeploymentScheduleJobService
    {
        private const string DeploymentScheduleTriggerJobId = "deployment-schedule-trigger-{0}";
        private const string DeploymentSchedulePhaseJobId = "deployment-schedule-phase-{0}";
        private const int FirstPhaseDelayMinutes = 5;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRecurringJobService _recurringJobService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly DeploymentScheduleOptions _options;
        private readonly IAssignmentProfileService _assignmentProfileService;

        public DeploymentScheduleService(
            IApplicationDbContext applicationDbContext,
            ILoggedInUserProvider loggedInUserProvider,
            IMapper mapper,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IRecurringJobService recurringJobService,
            IBackgroundJobService backgroundJobService,
            IDeployIntunewinService deployIntunewinService,
            IBackgroundJobClient backgroundJobClient,
            IOptions<DeploymentScheduleOptions> options, 
            IAssignmentProfileService assignmentProfileService)
        {
            _applicationDbContext = applicationDbContext;
            _loggedInUserProvider = loggedInUserProvider;
            _mapper = mapper;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _recurringJobService = recurringJobService;
            _backgroundJobService = backgroundJobService;
            _deployIntunewinService = deployIntunewinService;
            _backgroundJobClient = backgroundJobClient;
            _options = options.Value;
            _assignmentProfileService = assignmentProfileService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<long> AddAsync(DeploymentScheduleDetailsDto dto)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            DeploymentSchedule? newDeploymentSchedule = _mapper.Map<DeploymentSchedule>(dto);

            // Set the SubscriptionId of the new deployment schedule to the current subscription
            newDeploymentSchedule.SubscriptionId = subscriptionId;

            DeploymentSchedulePhase[]? phases = _mapper.Map<DeploymentSchedulePhase[]>(dto.Phases);

            // Add the new deployment schedule into the database
            newDeploymentSchedule = _applicationDbContext.DeploymentSchedules.Add(newDeploymentSchedule).Entity;

            await _applicationDbContext.SaveChangesAsync();

            // Add the selection phases of the new deployment schedule into the database
            for (int i = 0; i < phases.Length; i++)
            {
                DeploymentSchedulePhase? phase = phases[i];
                phase.DeploymentScheduleId = newDeploymentSchedule.Id;
                phase.OffsetDays = dto.Phases[i].OffsetDays;
                phase.AssignmentProfileId = dto.Phases[i].AssignmentProfileId;
                phase.Index = i;

                _applicationDbContext.DeploymentSchedulePhases.Add(phase);
            }

            await _applicationDbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(newDeploymentSchedule.CronTrigger))
            {
                var jobId = string.Format(DeploymentScheduleTriggerJobId, newDeploymentSchedule.Id);
                _recurringJobService.RegisterJob(jobId, () => TriggerJobAsync(loggedInUser,loggedInUser.SubscriptionId, newDeploymentSchedule.Id, null), newDeploymentSchedule.CronTrigger);
            }

            // Show notifications in audit log
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.DeploymentSchedules,
                string.Format(AuditLogActions.DeploymentSchedulesAddSchedule, dto.Name),
                author: loggedInUser);

            _log.Info($"Deployment schedule '{newDeploymentSchedule.Name}' with the id: {newDeploymentSchedule.Id} was added.");

            return newDeploymentSchedule.Id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="applicationIds"></param>
        /// <returns></returns>
        public async Task<int> AssignToPrivateApplicationsAsync(long id, DeploymentScheduleAssignmentDto dto)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Check if the deployment schedule already exists in the db, and save the profile in a variable
            DeploymentSchedule? deploymentSchedule = await _applicationDbContext.DeploymentSchedules.FindAsync(id);

            // Check if the deployment schedule is valid 
            if (deploymentSchedule == null)
            {
                _log.Error($"The deployment schedule was null when trying the assign action to a private application.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "The deployment schedule is null.");

                return StatusCodes.Status400BadRequest;
            }

            // Get the deployment schedule phases of the deployment schedule to be assigned
            DeploymentSchedulePhase[]? deploymentSchedulePhases = await _applicationDbContext
                .DeploymentSchedulePhases
                .Where(phase => phase.DeploymentSchedule.Id == id)
                .ToArrayAsync();

            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            PrivateApplication[]? applications = await _applicationDbContext
                .PrivateApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => dto.ApplicationIds.Contains(application.Id))
                .ToArrayAsync();

            bool containsNullIntuneId = applications.Any(application => application.IntuneId is null);

            if (containsNullIntuneId)
            {
                _log.Error("One or more intune ids of the private applications were null when trying to assign.");

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            bool inProgress = applications
                .Any(a => a
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any());

            if (inProgress)
            {
                _log.Warn("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are part of a deployment schedule in progress.");

                return StatusCodes.Status200OK;
            }

            foreach (PrivateApplication? application in applications)
            {
                application.DeploymentScheduleId = id;

                _log.Info($"The {application.Name} had a deployment schedule assigned successfully");

                await _notificationService.GenerateSuccessfulDeploymentScheduleAssignmentNotificationsAsync(
                    deploymentSchedule.Name,
                    application.Name,
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: true);

                await _auditLogService.GenerateAuditLogAsync(
                   AuditLogCategory.DeploymentSchedules,
                   string.Format(AuditLogActions.DeploymentSchedulesAssignSchedule, deploymentSchedule.Name, application.Name),
                   author: loggedInUser,
                   saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            if (deploymentSchedule.CronTrigger is null)
            {
                foreach (PrivateApplication? application in applications)
                {
                    if (application.DeploymentStatus != DeploymentStatus.SuccessfulNotUpToDate)
                    {
                        continue;
                    }

                    var app = (application.Id, true);
                    _backgroundJobClient.Enqueue(() => TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, id, app));
                }
            }

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="applicationIds"></param>
        /// <returns></returns>
        public async Task<int> AssignToPublicApplicationsAsync(long id, DeploymentScheduleAssignmentDto dto)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Check if the deployment schedule already exists in the db, and save the schedule in a variable
            DeploymentSchedule? deploymentSchedule = await _applicationDbContext.DeploymentSchedules.FindAsync(id);

            // Check if the deployment schedule is valid
            if (deploymentSchedule == null)
            {
                _log.Error($"The deployment schedule was null when trying the assign action to a public application.");

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "The deployment schedule is null.");

                return StatusCodes.Status400BadRequest;
            }

            // Get the deployment schedule phases of the deployment schedule to be assigned
            DeploymentSchedulePhase[]? deploymentSchedulePhases = await _applicationDbContext
                .DeploymentSchedulePhases
                .Where(phase => phase.DeploymentSchedule.Id == id)
                .ToArrayAsync();

            // Loop through all the applications in SubscriptionPublicApplication table and save in a list all applications that have an id in the applicationIds array
            SubscriptionPublicApplication[]? subscriptionPublicApplications = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(subscriptionPublicApplication => dto.ApplicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .Include(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplication)
                .ToArrayAsync();

            bool containsNullIntuneId = subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.IntuneId is null);
            bool containsUndeployedApplication = dto.ApplicationIds.Any(applicationId => !subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplicationId == applicationId));

            if (containsNullIntuneId || containsUndeployedApplication)
            {
                _log.Error("One or more intune ids of the public applications were null when trying to assign.");

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            bool inProgress = subscriptionPublicApplications
                .Any(a => a
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any());

            if (inProgress)
            {
                _log.Warn("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedDeploymentScheduleAssignmentNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are part of a deployment schedule in progress.");

                return StatusCodes.Status200OK;
            }

            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                subscriptionPublicApplication.DeploymentScheduleId = id;
                subscriptionPublicApplication.ManualApprove = false;

                if (dto.AutoUpdate)
                {
                    subscriptionPublicApplication.AutoUpdate = true;
                }

                _log.Info($"The {subscriptionPublicApplication.PublicApplication.Name} had a deployment schedule assigned successfully");

                await _notificationService.GenerateSuccessfulDeploymentScheduleAssignmentNotificationsAsync(
                    deploymentSchedule.Name,
                    subscriptionPublicApplication.PublicApplication.Name,
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: false);

                await _auditLogService.GenerateAuditLogAsync(
                    AuditLogCategory.DeploymentSchedules,
                    string.Format(AuditLogActions.DeploymentSchedulesAssignSchedule, deploymentSchedule.Name, subscriptionPublicApplication.PublicApplication.Name),
                    author: loggedInUser,
                    saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            if (deploymentSchedule.CronTrigger is null)
            {
                foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
                {
                    if (subscriptionPublicApplication.DeploymentStatus != DeploymentStatus.SuccessfulNotUpToDate)
                    {
                        continue;
                    }

                    if (!subscriptionPublicApplication.AutoUpdate)
                    {
                        continue;
                    }

                    var app = (subscriptionPublicApplication.PublicApplicationId, false);
                    _backgroundJobClient.Enqueue(() => TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, id, app));
                }
            }

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationIds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> ClearFromPrivateApplicationsAsync(DeploymentScheduleClearDto dto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            List<PrivateApplication>? applications = await _applicationDbContext
                .PrivateApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => dto.ApplicationIds.Contains(application.Id))
                .Include(application => application.DeploymentScheduleApplications)
                .ToListAsync();

            bool containsNullIntuneId = applications.Any(application => application.IntuneId is null);

            if (containsNullIntuneId)
            {
                _log.Warn("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedClearDeploymentScheduleNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            bool inProgress = applications
                .Any(a => a
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Where(a => a.PhaseState == PhaseState.InProgress)
                    .Any());

            if (inProgress)
            {
                _log.Warn("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedClearDeploymentScheduleNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are part of a deployment schedule phase in progress.");

                return StatusCodes.Status200OK;
            }

            await ClearFromPrivateApplicationsNoCheckAsync(loggedInUser, dto);

            return StatusCodes.Status200OK;
        }

        public async Task<int> ClearFromPrivateApplicationsNoCheckAsync(UserDto loggedInUser, DeploymentScheduleClearDto dto)
        {
            List<PrivateApplication>? applications = await _applicationDbContext
                .PrivateApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => dto.ApplicationIds.Contains(application.Id))
                .ToListAsync();

            foreach (PrivateApplication? application in applications)
            {
                if (application.DeploymentSchedule is null)
                {
                    continue;
                }

                application.DeploymentScheduleId = null;
                var deploymentScheduleApplications = application
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Where(a => a.PhaseState != PhaseState.InProgress);

                foreach (var deploymentScheduleApplication in deploymentScheduleApplications)
                {
                    deploymentScheduleApplication.Type = DeploymentScheduleApplicationType.Previous;
                    deploymentScheduleApplication.PhaseState = PhaseState.Finished;
                }

                _log.Info($"The {application.Name} had a deployment schedule cleared successfully");

                await _notificationService.GenerateSuccessfulClearDeploymentScheduleNotificationsAsync(
                    application.Name,
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: true);

                await _auditLogService.GenerateAuditLogAsync(
                   AuditLogCategory.DeploymentSchedules,
                   string.Format(AuditLogActions.DeploymentSchedulesClearSchedule, application.Name),
                   author: loggedInUser,
                   saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            if (dto.ShouldRemovePatchApp)
            {
                var deploymentSchedulePrivateApplications = await _applicationDbContext
                    .DeploymentSchedulePrivateApplications
                    .Where(d => dto.ApplicationIds.Contains(d.ApplicationId))
                    .ToArrayAsync();

                foreach (var deploymentSchedulePrivateApplication in deploymentSchedulePrivateApplications)
                {
                    await _deployIntunewinService.RemoveDeployedApplicationAsync(loggedInUser.SubscriptionId, deploymentSchedulePrivateApplication.IntuneId);
                }

                _applicationDbContext.DeploymentSchedulePrivateApplications.RemoveRange(deploymentSchedulePrivateApplications);

                await _applicationDbContext.SaveChangesAsync();
            }

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationIds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> ClearFromPublicApplicationsAsync(DeploymentScheduleClearDto dto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Loop through all the applications in PrivateApplications table and save in a list all applications that have an id in the applicationIds array
            SubscriptionPublicApplication[]? subscriptionPublicApplications = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(subscriptionPublicApplication => dto.ApplicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .ToArrayAsync();

            bool containsNullIntuneId = subscriptionPublicApplications.Any(application => application.IntuneId is null);
            bool containsUndeployedApplication = dto.ApplicationIds.Any(applicationId => !subscriptionPublicApplications.Any(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplicationId == applicationId));

            if (containsNullIntuneId || containsUndeployedApplication)
            {
                _log.Error("One or more intune ids of the public applications were null when trying to clear.");

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedClearDeploymentScheduleNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: false,
                    extraErrorMessage: "One or more applications are not deployed.");

                return StatusCodes.Status200OK;
            }

            bool inProgress = subscriptionPublicApplications
                .Any(a => a
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Where(a => a.PhaseState == PhaseState.InProgress)
                    .Any());

            if (inProgress)
            {
                _log.Warn("One or more intune ids of the private applications were null when trying to clear.");

                string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

                // Show notifications for failed clearing the deployment schedule
                await _notificationService.GenerateFailedClearDeploymentScheduleNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    authorId: superAdminId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "One or more applications are part of a deployment schedule in progress.");

                return StatusCodes.Status200OK;
            }

            await ClearFromPublicApplicationsNoCheckAsync(loggedInUser, dto);

            return StatusCodes.Status200OK;
        }

        public async Task<int> ClearFromPublicApplicationsNoCheckAsync(UserDto loggedInUser, DeploymentScheduleClearDto dto)
        {
            List<SubscriptionPublicApplication>? subscriptionPublicApplications = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(application => application.SubscriptionId == loggedInUser.SubscriptionId)
                .Where(application => dto.ApplicationIds.Contains(application.PublicApplicationId))
                .ToListAsync();

            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                if (subscriptionPublicApplication.DeploymentSchedule is null)
                {
                    continue;
                }

                subscriptionPublicApplication.DeploymentScheduleId = null;

                var deploymentScheduleApplications = subscriptionPublicApplication
                    .DeploymentScheduleApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Where(a => a.PhaseState != PhaseState.InProgress);

                foreach (var deploymentScheduleApplication in deploymentScheduleApplications)
                {
                    deploymentScheduleApplication.Type = DeploymentScheduleApplicationType.Previous;
                    deploymentScheduleApplication.PhaseState = PhaseState.Finished;
                }

                _log.Info($"The {subscriptionPublicApplication.PublicApplication.Name} had a deployment schedule cleared successfully");

                await _notificationService.GenerateSuccessfulClearDeploymentScheduleNotificationsAsync(
                    subscriptionPublicApplication.PublicApplication.Name,
                    loggedInUser.SubscriptionId,
                    authorId: loggedInUser.Id,
                    isForPrivateRepository: true);

                await _auditLogService.GenerateAuditLogAsync(
                   AuditLogCategory.DeploymentSchedules,
                   string.Format(AuditLogActions.DeploymentSchedulesClearSchedule, subscriptionPublicApplication.PublicApplication.Name),
                   author: loggedInUser,
                   saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            if (dto.ShouldRemovePatchApp)
            {
                var deploymentScheduleSubscribtionPublicApplications = await _applicationDbContext
                    .DeploymentScheduleSubscriptionPublicApplications
                    .Where(d => d.SubscriptionId == loggedInUser.SubscriptionId)
                    .Where(d => dto.ApplicationIds.Contains(d.ApplicationId))
                    .ToArrayAsync();

                foreach (var deploymentScheduleSubscribtionPublicApplication in deploymentScheduleSubscribtionPublicApplications)
                {
                    await _deployIntunewinService.RemoveDeployedApplicationAsync(loggedInUser.SubscriptionId, deploymentScheduleSubscribtionPublicApplication.IntuneId);
                }

                _applicationDbContext.DeploymentScheduleSubscriptionPublicApplications.RemoveRange(deploymentScheduleSubscribtionPublicApplications);

                await _applicationDbContext.SaveChangesAsync();
            }

            return  StatusCodes.Status200OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<int> CopyAsync(long[] ids)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the deployment schedules that we want to copy
            DeploymentSchedule[]? deploymentSchedulesToCopy = await _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => ids.Contains(deploymentSchedule.Id))
                .ToArrayAsync();

            // Check if the deployment schedule is valid
            if (deploymentSchedulesToCopy.Length != ids.Length)
            {
                _log.Error($"One ore more deployment schedule ids are invalid.");
                return StatusCodes.Status400BadRequest;
            }

            List<EntityEntry<DeploymentSchedule>>? newDeploymentSchedules = new List<EntityEntry<DeploymentSchedule>>(deploymentSchedulesToCopy.Length);

            foreach (DeploymentSchedule? deploymentSchedule in deploymentSchedulesToCopy)
            {
                var name = deploymentSchedule.Name + Utils.CopySuffix;

                if (name.Length > Validation.NameMaxLength)
                {
                    name = deploymentSchedule.Name;
                }

                // Create a new empty deployment schedule and copy the properties of the deployment schedule that we want to copy
                DeploymentSchedule? copiedDeploymentSchedule = new DeploymentSchedule
                {
                    Name = name,
                    SubscriptionId = deploymentSchedule.SubscriptionId,
                    CronTrigger = deploymentSchedule.CronTrigger
                };

                EntityEntry<DeploymentSchedule>? newDeploymentSchedule = _applicationDbContext.DeploymentSchedules.Add(copiedDeploymentSchedule);
                newDeploymentSchedules.Add(newDeploymentSchedule);
            }

            await _applicationDbContext.SaveChangesAsync();

            var deploymentSchedulesNewAndOriginal = newDeploymentSchedules.Zip(
                deploymentSchedulesToCopy,
                (newDeploymentSchedule, originalDeploymentSchedule) => new { NewDeploymentSchedule = newDeploymentSchedule, OriginalDeploymentSchedule = originalDeploymentSchedule }
                );

            foreach (var deploymentScheduleNewAndOriginal in deploymentSchedulesNewAndOriginal)
            {
                DeploymentSchedule? newDeploymentSchedule = deploymentScheduleNewAndOriginal.NewDeploymentSchedule.Entity;
                DeploymentSchedule? originalDeploymentSchedule = deploymentScheduleNewAndOriginal.OriginalDeploymentSchedule;

                if (!string.IsNullOrEmpty(newDeploymentSchedule.CronTrigger))
                {
                    var jobId = string.Format(DeploymentScheduleTriggerJobId, newDeploymentSchedule.Id);
                    _recurringJobService.RegisterJob(jobId, () => TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, newDeploymentSchedule.Id, null), newDeploymentSchedule.CronTrigger);
                }

                // Get the phases of the deployment schedule that we want to copy and store them into a variable
                List<DeploymentSchedulePhase>? deploymentSchedulePhases = await _applicationDbContext
                    .DeploymentSchedulePhases
                    .Where(phase => phase.DeploymentScheduleId == originalDeploymentSchedule.Id)
                    .ToListAsync();

                // Loop through each phase of the deployment schedule we want to copy, copy them, and add them in the db
                foreach (DeploymentSchedulePhase? phase in deploymentSchedulePhases)
                {
                    // Copy the phases of the deployment schedule we want to copy
                    DeploymentSchedulePhase? phaseToBeCopied = new DeploymentSchedulePhase
                    {
                        Name = phase.Name,
                        OffsetDays = phase.OffsetDays,
                        DeploymentScheduleId = newDeploymentSchedule.Id,
                        AssignmentProfileId = phase.AssignmentProfileId,
                        UseRequirementScript = phase.UseRequirementScript
                    };

                    _applicationDbContext.DeploymentSchedulePhases.Add(phaseToBeCopied);
                }
            }

            await _applicationDbContext.SaveChangesAsync();


            foreach (DeploymentSchedule? deploymentSchedule in deploymentSchedulesToCopy)
            {
                // Show notifications in audit log
                await _auditLogService.GenerateAuditLogAsync(
                  AuditLogCategory.DeploymentSchedules,
                  string.Format(AuditLogActions.DeploymentSchedulesCopySchedule, deploymentSchedule.Name),
                  author: loggedInUser,
                  saveChanges: false);

                _log.Info($"The deployment schedule: '{deploymentSchedule.Name}', with id: {deploymentSchedule.Id} was copied.");
            }

            await _applicationDbContext.SaveChangesAsync();

            return StatusCodes.Status200OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<Result> EditAsync(long id, DeploymentScheduleDetailsDto dto)
        {
            // Get the logged in user
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the deployment schedule that we want to edit
            DeploymentSchedule? deploymentScheduleToEdit = await _applicationDbContext
                .DeploymentSchedules
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePhases)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications)
                .Where(deploymentSchedule => deploymentSchedule.Id == id && deploymentSchedule.SubscriptionId == loggedInUser!.SubscriptionId)
                .FirstOrDefaultAsync();

            // Check if the deployment schedule is valid
            if (deploymentScheduleToEdit == null)
            {
                _log.Error($"The deployment schedule was null when trying to edit.");
                return new NotFoundResult("Deployment schedule not found.");
            }

            var inProgress = deploymentScheduleToEdit
                    .DeploymentSchedulePrivateApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any() ||
                    deploymentScheduleToEdit.DeploymentScheduleSubscriptionPublicApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any();

            if (inProgress)
            {
                _log.Error($"The deployment schedule was in progress when trying to edit.");
                return new InvalidOperationResult("Deployment schedule is in progress.");
            }

            // Get the phases of the edited deployment schedule and store them into a variable
            List<DeploymentSchedulePhase>? deploymentScheduleToEditPhases = await _applicationDbContext
                .DeploymentSchedulePhases
                .Where(phase => phase.DeploymentScheduleId == id)
                .ToListAsync();

            // Set the name and the last updated property of the edited deployment schedule
            deploymentScheduleToEdit.Name = dto.Name;
            deploymentScheduleToEdit.CronTrigger = dto.CronTrigger;
            deploymentScheduleToEdit.ModifiedOn = DateTime.UtcNow;

            // Map the phases of the new edited deployment schedule
            List<DeploymentSchedulePhase>? phases = _mapper.Map<List<DeploymentSchedulePhase>>(dto.Phases);

            // Add the new phases to the edited deployment schedule
            for (int i = 0; i < phases.Count; i++)
            {
                DeploymentSchedulePhase? phase = phases[i];
                phase.Index = i;
            }

            deploymentScheduleToEdit.DeploymentSchedulePhases = phases;

            await _applicationDbContext.SaveChangesAsync();
            var jobId = string.Format(DeploymentScheduleTriggerJobId, deploymentScheduleToEdit.Id);

            if (!string.IsNullOrEmpty(deploymentScheduleToEdit.CronTrigger))
            {
                _recurringJobService.RegisterJob(jobId, () => TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, deploymentScheduleToEdit.Id, null), deploymentScheduleToEdit.CronTrigger);
            }
            else
            {
                _recurringJobService.RemoveJob(jobId);
            }

            string? superAdminId = loggedInUser.UserRole == UserRole.SuperAdmin ? loggedInUser.Id : null;

            // Show notifications in audit log
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.DeploymentSchedules,
                string.Format(AuditLogActions.DeploymentSchedulesEditSchedule, deploymentScheduleToEdit.Name),
                author: loggedInUser,
                saveChanges: false);

            _log.Info($"The deployment schedule: '{deploymentScheduleToEdit.Name}' with the id: {deploymentScheduleToEdit.Id} was edited.");

            return new SuccessResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DeploymentScheduleDto>> FilterByNameAsync(string? name)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get all the assignment profiles of the subscription
            IQueryable<DeploymentSchedule>? deploymentSchedules = _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => deploymentSchedule.SubscriptionId == subscriptionId)
                .Where(deploymentSchedules => !deploymentSchedules.IsDeleted);

            // If the users types something in search box, return the assignment profiles that name starts with the letter the user typed and then order
            // them by name
            if (!string.IsNullOrEmpty(name))
            {
                return await deploymentSchedules
                    .Where(deploymentSchedule => deploymentSchedule.Name.ToLower().Contains(name.ToLower()))
                    .OrderBy(deploymentSchedule => deploymentSchedule.Name)
                    .ProjectTo<DeploymentScheduleDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();
            }

            // If there is no search term than return all the assignment profiles order by name
            return await deploymentSchedules
                .OrderBy(deploymentSchedule => deploymentSchedule.Name)
                .ProjectTo<DeploymentScheduleDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the deployment schedule based on it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<DeploymentScheduleDetailsDto> GetAsync(long id)
        {
            // Get the logged in user
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get the deployment schedule we want by the given id
            DeploymentSchedule? deploymentSchedule = await _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => deploymentSchedule.Id == id && deploymentSchedule.SubscriptionId == subscriptionId)
                .FirstOrDefaultAsync();

            // Check if the deployment schedule exists
            if (deploymentSchedule == null)
            {
                _log.Error($"Deployment schedule with the id:{id}, does not exist");
                throw new Exception($"Deployment schedule with the id:{id}, does not exist");
            }

            // Get all the phases corresponding to the deployment schedule we want to get
            DeploymentSchedulePhase[]? deploymentSchedulePhases = await _applicationDbContext
               .DeploymentSchedulePhases
               .Where(phase => phase.DeploymentScheduleId == id)
               .Include(phase => phase.AssignmentProfile)
               .ToArrayAsync();

            // Map the deployment schedule we want to return to the one we found on the db with the same id
            DeploymentScheduleDetailsDto? deploymentScheduleDetails = _mapper.Map<DeploymentScheduleDetailsDto>(deploymentSchedule);

            // Create a phases array that we will put on the deployment schedule that we want to return
            DeploymentSchedulePhaseDto[] deploymentSchedulePhasesArray = new DeploymentSchedulePhaseDto[deploymentSchedulePhases.Length];

            // Populate the array with the correct phases taken from the db based on their deployment schedule id
            for (int i = 0; i < deploymentSchedulePhases.Length; i++)
            {
                DeploymentSchedulePhaseDto deploymentSchedulePhasesDto = _mapper.Map<DeploymentSchedulePhaseDto>(deploymentSchedulePhases[i]);
                deploymentSchedulePhasesArray[i] = deploymentSchedulePhasesDto;
            }

            // Add the phases array to the deployment schedule that we want to return
            deploymentScheduleDetails.Phases = deploymentSchedulePhasesArray;

            return deploymentScheduleDetails;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<long>> ListIdsAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            return await _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => deploymentSchedule.SubscriptionId == subscriptionId)
                .Select(deploymentSchedule => deploymentSchedule.Id)
                .ToListAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public async Task<PagedResult<DeploymentScheduleDto>> ListPagedAsync(int pageNumber, int pageSize, string searchTerm)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            // Get all the deployment Schedule that the given subscription contains
            IQueryable<DeploymentSchedule>? queryDeploymentSchedules = _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => deploymentSchedule.SubscriptionId == subscriptionId)
                .Where(deploymentSchedule => !deploymentSchedule.IsDeleted);

            // Filter the deployment schedules shown by the input of the search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryDeploymentSchedules = queryDeploymentSchedules
                    .Where(deploymentSchedule => deploymentSchedule.Name.Contains(searchTerm.Trim()));
            }

            // Take all the deployment schedules from this subscription order them based on ModifiedOn and Name property an do the paginating
            List<DeploymentSchedule>? deploymentSchedules = await queryDeploymentSchedules
                    .OrderByDescending(deploymentSchedule => deploymentSchedule.ModifiedOn)
                    .ThenBy(deploymentSchedule => deploymentSchedule.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePrivateApplications)
                    .Include(deploymentSchedule => deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications)
                    .ToListAsync();

            List<DeploymentScheduleDto>? deploymentScheduleDtos = _mapper.Map<List<DeploymentScheduleDto>>(deploymentSchedules);

            // Take all deployment schedules on the page
            for (int i = 0; i < deploymentSchedules.Count; i++)
            {
                var deploymentSchedule = deploymentSchedules[i];
                var deploymentScheduleDto = deploymentScheduleDtos[i];

                // Count the private applications that have a deployment schedule assigned
                int assignedPrivateApplications = deploymentSchedule.PrivateApplications.Count;

                //// Count the public applications that have a deployment schedule assigned
                int assignedPublicApplications = deploymentSchedule.SubscriptionPublicApplications.Count;

                //// Set the number of applications assigned of this profile to be the sum between the number of private applications
                //// assigned to this profile and the number of public applications assigned to this profile
                deploymentScheduleDto.NumberOfApplicationsAssigned = assignedPrivateApplications + assignedPublicApplications;
                deploymentScheduleDto.CurrentPhaseName = deploymentSchedule.DeploymentSchedulePhases.Where(p => p.Index == deploymentSchedule.CurrentPhaseIndex).FirstOrDefault()?.Name;
                deploymentScheduleDto.IsInProgress = deploymentSchedule
                    .DeploymentSchedulePrivateApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any() ||
                    deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                    .Any();
            }

            return new PagedResult<DeploymentScheduleDto>
            {
                AllItemsCount = await queryDeploymentSchedules.CountAsync(),
                PageItems = deploymentScheduleDtos
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assignmentIds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> RemoveAsync(DeploymentScheduleRemoveDto dto)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            // Get the deployment schedules we want to delete
            DeploymentSchedule[] deploymentSchedulesToBeDeleted = await _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => dto.Ids.Contains(deploymentSchedule.Id))
                .Include(deploymentSchedule => deploymentSchedule.PrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.SubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePhases)
                .ToArrayAsync();

            // Check if the profile exists
            if (deploymentSchedulesToBeDeleted.Length != dto.Ids.Length)
            {
                _log.Error($"One ore more deployment schedule ids are invalid.");
                return StatusCodes.Status400BadRequest;
            }

            // Check if it the deployment schedule is from the current subscription
            foreach (DeploymentSchedule deploymentSchedule in deploymentSchedulesToBeDeleted)
            {
                if (deploymentSchedule.SubscriptionId != loggedInUser.SubscriptionId)
                {
                    _log.Error($"The deployment schedule with the id: {deploymentSchedule.Id}, is not from the current subscription");
                    return StatusCodes.Status401Unauthorized;
                }
            }

            // Delete all the deployment schedules that are in the list of deployment schedules to be deleted and show audit log notifications and log action
            foreach (DeploymentSchedule? deploymentSchedule in deploymentSchedulesToBeDeleted)
            {
                deploymentSchedule.IsDeleted = true;
            }

            await _applicationDbContext.SaveChangesAsync();

            foreach (DeploymentSchedule? deploymentSchedule in deploymentSchedulesToBeDeleted)
            {
                var jobId = string.Format(DeploymentScheduleTriggerJobId, deploymentSchedule.Id);
                _recurringJobService.RemoveJob(jobId);
            }

            foreach (DeploymentSchedule? deploymentSchedule in deploymentSchedulesToBeDeleted)
            {
                _backgroundJobService.Enqueue(() => RemoveJobAsync(loggedInUser, deploymentSchedule.Id, dto.ShouldRemovePatchApp));
            }

            return StatusCodes.Status200OK;
        }

        public async Task DeletePrivateApplicationPatchAppsAsync(int[] ids)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            string[] deploymentSchedulePrivateApplicationIntuneIds = await _applicationDbContext
                    .DeploymentSchedulePrivateApplications
                    .Where(d => d.Application.SubscriptionId == loggedInUser.SubscriptionId)
                    .Where(d => d.Type == DeploymentScheduleApplicationType.Previous)
                    .Where(d => ids.Contains(d.ApplicationId))
                    .Select(d => d.IntuneId)
                    .ToArrayAsync();

            _backgroundJobService.Enqueue(() => DeletePatchAppsJobAsync(loggedInUser.SubscriptionId, deploymentSchedulePrivateApplicationIntuneIds));
        }

        public async Task DeletePublicApplicationPatchAppsAsync(int[] ids)
        {
            UserDto loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            string[] deploymentScheduleSubscribtionPublicApplicationIntuneIds = await _applicationDbContext
                    .DeploymentScheduleSubscriptionPublicApplications
                    .Where(d => d.SubscriptionId == loggedInUser.SubscriptionId)
                    .Where(d => d.Type == DeploymentScheduleApplicationType.Previous)
                    .Where(d => ids.Contains(d.ApplicationId))
                    .Select(d => d.IntuneId)
                    .ToArrayAsync();

            _backgroundJobService.Enqueue(() => DeletePatchAppsJobAsync(loggedInUser.SubscriptionId, deploymentScheduleSubscribtionPublicApplicationIntuneIds));

        }

        public async Task DeletePatchAppsJobAsync(Guid subscriptionId, string[] intuneIds)
        {
            foreach (string intuneId in intuneIds)
            {
                await _deployIntunewinService.RemoveDeployedApplicationAsync(subscriptionId, intuneId);
            }
        }

        public async Task RemoveJobAsync(UserDto loggedInUser, long id, bool shouldRemovePatchApp)
        {
            DeploymentSchedule? deploymentScheduleToBeDeleted = await _applicationDbContext
                .DeploymentSchedules
                .Where(deploymentSchedule => deploymentSchedule.Id == id)
                .Include(deploymentSchedule => deploymentSchedule.PrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.SubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePhases)
                .FirstOrDefaultAsync();

            if (deploymentScheduleToBeDeleted is null)
            {
                return;
            }

            var privateClearDto = new DeploymentScheduleClearDto
            {
                ApplicationIds = deploymentScheduleToBeDeleted.PrivateApplications.Select(a => a.Id).ToArray(),
                ShouldRemovePatchApp = shouldRemovePatchApp,
            };

            var publicClearDto = new DeploymentScheduleClearDto
            {
                ApplicationIds = deploymentScheduleToBeDeleted.SubscriptionPublicApplications.Select(a => a.PublicApplicationId).ToArray(),
                ShouldRemovePatchApp = shouldRemovePatchApp,
            };

            await ClearFromPrivateApplicationsNoCheckAsync(loggedInUser, privateClearDto);
            await ClearFromPublicApplicationsNoCheckAsync(loggedInUser, publicClearDto);

            _applicationDbContext.DeploymentSchedules.Remove(deploymentScheduleToBeDeleted);

            //Show notifications in audit log
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.DeploymentSchedules,
                string.Format(AuditLogActions.DeploymentSchedulesRemoveSchedule, deploymentScheduleToBeDeleted.Name),
                author: loggedInUser,
                saveChanges: false);

            _log.Info($"Removed the deployment schedule '{deploymentScheduleToBeDeleted.Name}' with id: {deploymentScheduleToBeDeleted.Id} and the patch-app instances linked to it.");

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task TriggerJobAsync(UserDto loggedInUser, Guid subscriptionId, long deploymentScheduleId, (int Id, bool IsPrivate)? application = null)
        {
            DeploymentSchedule? schedule = await _applicationDbContext.DeploymentSchedules
                .Include(deploymentSchedule => deploymentSchedule.PrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.SubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePrivateApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentScheduleSubscriptionPublicApplications)
                .Include(deploymentSchedule => deploymentSchedule.DeploymentSchedulePhases)
                .Where(schedule => schedule.Id == deploymentScheduleId)
                .FirstOrDefaultAsync();

            if (schedule is null)
            {
                return;
            }

            var assignedPrivateApplications = Array.Empty<PrivateApplication>();
            var assignedPublicApplications = Array.Empty<SubscriptionPublicApplication>();

            if (application is null)
            {
                assignedPrivateApplications = schedule
                    .PrivateApplications
                    .Where(application => application.SubscriptionId == subscriptionId &&
                        application.DeploymentStatus == DeploymentStatus.SuccessfulNotUpToDate
                    )
                    .ToArray();

                assignedPublicApplications = schedule
                    .SubscriptionPublicApplications
                    .Where(application => application.SubscriptionId == subscriptionId &&
                        application.DeploymentStatus == DeploymentStatus.SuccessfulNotUpToDate &&
                        application.AutoUpdate
                    )
                    .ToArray();
            }
            else
            {
                if (application.Value.IsPrivate)
                {
                    var app = schedule
                        .PrivateApplications
                        .Where(a => a.SubscriptionId == subscriptionId &&
                            a.Id == application.Value.Id &&
                            a.DeploymentStatus == DeploymentStatus.SuccessfulNotUpToDate
                        )
                        .FirstOrDefault();

                    if (app is not null)
                    {
                        assignedPrivateApplications = new[] { app };
                    }
                }
                else
                {
                    var app = schedule
                        .SubscriptionPublicApplications
                        .Where(a => a.SubscriptionId == subscriptionId &&
                            a.PublicApplicationId == application.Value.Id &&
                            a.DeploymentStatus == DeploymentStatus.SuccessfulNotUpToDate &&
                            (a.AutoUpdate || a.ManualApprove))
                        .FirstOrDefault();

                    if (app is not null)
                    {
                        assignedPublicApplications = new[] { app };
                    }
                }
            }

            DeploymentSchedulePhase firstPhase = schedule.DeploymentSchedulePhases
                .Where(phase => phase.Index == 0)
                .First();

            foreach (var assignedPrivateApplication in assignedPrivateApplications)
            {
                _applicationDbContext.DeploymentSchedulePrivateApplications.Add(new DeploymentSchedulePrivateApplication
                {
                    ApplicationId = assignedPrivateApplication.Id,
                    DeploymentScheduleId = deploymentScheduleId,
                    Type = DeploymentScheduleApplicationType.Current,
                    Version = assignedPrivateApplication.Version
                });
            }

            foreach (var assignedPublicApplication in assignedPublicApplications)
            {
                _applicationDbContext.DeploymentScheduleSubscriptionPublicApplications.Add(new DeploymentScheduleSubscriptionPublicApplication
                {
                    ApplicationId = assignedPublicApplication.PublicApplicationId,
                    SubscriptionId = subscriptionId,
                    DeploymentScheduleId = deploymentScheduleId,
                    Type = DeploymentScheduleApplicationType.Current,
                });
            }

            if (assignedPrivateApplications.Length > 0 || assignedPublicApplications.Length > 0)
            {
                _log.Info($"The {schedule.Name} had started successfully");

                await _notificationService.GenerateSuccessfulStartDeploymentScheduleNotificationsAsync(
                    loggedInUser.SubscriptionId,
                    schedule.Name,
                    authorId: loggedInUser.Id);

                await _auditLogService.GenerateAuditLogAsync(
                   AuditLogCategory.DeploymentSchedules,
                   string.Format(AuditLogActions.DeploymentSchedulesStartSchedule, schedule.Name),
                   saveChanges: false);
            }

            await _applicationDbContext.SaveChangesAsync();

            DateTimeOffset date = DateTimeOffset.UtcNow.AddDays(firstPhase.OffsetDays * _options.OffsetDays);

            if (assignedPrivateApplications.Length > 0)
            {
                var ids = assignedPrivateApplications.Select(a => a.Id).ToArray();
                _backgroundJobService.Schedule(() => PrivatePhaseJobAsync(loggedInUser, subscriptionId, firstPhase.Id, ids, date), date);
            }

            if (assignedPublicApplications.Length > 0)
            {
                var ids = assignedPublicApplications.Select(a => a.PublicApplicationId).ToArray();
                _backgroundJobService.Schedule(() => PublicPhaseJobAsync(loggedInUser, subscriptionId, firstPhase.Id, ids, date), date);
            }
        }

        /// <summary>
        /// The action that the phase should do
        /// </summary>
        /// <param name="loggedInUser"></param>
        /// <param name="phaseId"></param>
        /// <returns></returns>
        public async Task PrivatePhaseJobAsync(UserDto loggedInUser, Guid subscriptionId, long phaseId, int[] applicationIds, DateTimeOffset firstPhaseDate)
        {
            DeploymentSchedulePhase? phase = await _applicationDbContext.DeploymentSchedulePhases
                .Include(p => p.DeploymentSchedule)
                .ThenInclude(d => d.DeploymentSchedulePrivateApplications)
                .ThenInclude(a => a.Application)
                .Where(p => p.Id == phaseId)
                .FirstOrDefaultAsync();

            if (phase is null)
            {
                return;
            }

            if (phase.DeploymentSchedule.CronTrigger is not null)
            {
                phase.DeploymentSchedule.CurrentPhaseIndex = phase.Index;
            }

            var applications = phase
                .DeploymentSchedule
                .PrivateApplications
                .SelectMany(a => a.DeploymentScheduleApplications)
                .Where(a => applicationIds.Contains(a.ApplicationId))
                .ToArray();

            var currentApplications = applications
               .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
               .ToArray();

            applicationIds = currentApplications
                .Select(a => a.ApplicationId)
                .ToArray();

            if (applicationIds.Length == 0)
            {
                return;
            }

            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext
                .AssignmentProfileGroups
                .Where(group => group.AssignmentProfile.Id == phase.AssignmentProfileId)
                .ToArrayAsync();

            foreach (var application in currentApplications)
            {
                application.PhaseState = PhaseState.InProgress;
                application.CurrentPhaseId = phase.Id;
            }

            await _applicationDbContext.SaveChangesAsync();

            // first phase
            if (phase.Index == 0)
            {
                foreach (var application in currentApplications)
                {
                    application.Version = application.Application.Version;
                    application.IntuneId = await _deployIntunewinService.PublishPrivateAppToIntuneAsNewAsync(subscriptionId, application.ApplicationId);
                }

                await _applicationDbContext.SaveChangesAsync();
                await Task.Delay((int)TimeSpan.FromMinutes(FirstPhaseDelayMinutes).TotalMilliseconds);
            }

            foreach (var currentApplication in currentApplications)
            {
                await _deployIntunewinService.TogglePrivateRequirementScriptAsync(loggedInUser.SubscriptionId, currentApplication.ApplicationId, currentApplication.IntuneId, phase.UseRequirementScript);
            }

            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = currentApplications
                .Select(d => new { d.Application, d.IntuneId })
                .Select(group => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = group.Application.SubscriptionId,
                    IntuneId = group.IntuneId,
                    Name = group.Application.Name,
                    IsPrivate = true,
                })
                .ToArray();

            if (phase.AssignmentProfile is not null)
            {
                await _assignmentProfileService.AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, phase.AssignmentProfile, assignmentProfileApplicationDtos, assignmentProfileGroups, generateNotifications: false);
            }

            // last phase
            if (phase.Index == phase.DeploymentSchedule.DeploymentSchedulePhases.Count - 1)
            {
                var previousApplications = phase
                    .DeploymentSchedule
                    .PrivateApplications
                    .Where(a => applicationIds.Contains(a.Id))
                    .SelectMany(a => a.DeploymentScheduleApplications)
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Previous)
                    .ToArray();

                foreach (var application in previousApplications)
                {
                    await _deployIntunewinService.RemoveDeployedApplicationAsync(subscriptionId, application.IntuneId);
                }

                _applicationDbContext.DeploymentSchedulePrivateApplications.RemoveRange(previousApplications);

                await _applicationDbContext.SaveChangesAsync();

                foreach (var application in currentApplications)
                {
                    var shouldTriggerAgain = phase.DeploymentSchedule.CronTrigger is null;
                    _backgroundJobService.Enqueue(() => DeployCurrentPrivateApplicationAsync(loggedInUser, application.ApplicationId, application.ArchiveFileName, application.Version, shouldTriggerAgain));
                }

                foreach (var application in currentApplications)
                {
                    application.Type = DeploymentScheduleApplicationType.Previous;
                    application.PhaseState = PhaseState.Finished;
                }
            }
            else
            {
                if (applicationIds.Length == 0)
                {
                    return;
                }

                var nextPhase = phase
                    .DeploymentSchedule
                    .DeploymentSchedulePhases
                    .Where(p => p.Index > phase.Index)
                    .OrderBy(p => p.Index)
                    .FirstOrDefault();

                if (nextPhase is null)
                {
                    return;
                }

                DateTimeOffset date = firstPhaseDate.AddDays(nextPhase.OffsetDays * _options.OffsetDays);

                _backgroundJobService.Schedule(() => PrivatePhaseJobAsync(loggedInUser, subscriptionId, nextPhase.Id, applicationIds, firstPhaseDate), date);

                foreach (var currentApplication in currentApplications)
                {
                    currentApplication.PhaseState = PhaseState.Finished;
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task PublicPhaseJobAsync(UserDto loggedInUser, Guid subscriptionId, long phaseId, int[] applicationIds, DateTimeOffset firstPhaseDate)
        {
            DeploymentSchedulePhase? phase = await _applicationDbContext.DeploymentSchedulePhases
                .Include(p => p.AssignmentProfile)
                .Include(p => p.DeploymentSchedule)
                .ThenInclude(d => d.DeploymentScheduleSubscriptionPublicApplications)
                .ThenInclude(d => d.Application)
                .Where(p => p.Id == phaseId)
                .FirstOrDefaultAsync();

            if (phase is null)
            {
                return;
            }

            if (phase.DeploymentSchedule.CronTrigger is not null)
            {
                phase.DeploymentSchedule.CurrentPhaseIndex = phase.Index;
            }

            var applications = phase
                .DeploymentSchedule
                .SubscriptionPublicApplications
                .SelectMany(a => a.DeploymentScheduleApplications)
                .Where(a => applicationIds.Contains(a.ApplicationId))
                .ToArray();

            var currentApplications = applications
               .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
               .ToArray();

            applicationIds = currentApplications
                .Select(a => a.ApplicationId)
                .ToArray();

            if (applicationIds.Length == 0)
            {
                return;
            }

            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(group => group.AssignmentProfile.Id == phase.AssignmentProfileId)
                .ToArrayAsync();

            foreach (var application in currentApplications)
            {
                application.PhaseState = PhaseState.InProgress;
                application.CurrentPhaseId = phase.Id;
            }

            await _applicationDbContext.SaveChangesAsync();

            // first phase
            if (phase.Index == 0)
            {

                foreach (var application in currentApplications)
                {
                    application.Version = application.Application.PublicApplication.Version;
                    application.IntuneId = await _deployIntunewinService.PublishPublicAppToIntuneAsNewAsync(subscriptionId, application.ApplicationId);
                }

                await _applicationDbContext.SaveChangesAsync();
                await Task.Delay((int)TimeSpan.FromMinutes(FirstPhaseDelayMinutes).TotalMilliseconds);
            }

            foreach (var currentApplication in currentApplications)
            {
                await _deployIntunewinService.TogglePublicRequirementScriptAsync(loggedInUser.SubscriptionId, currentApplication.ApplicationId, currentApplication.IntuneId, phase.UseRequirementScript);
            }

            AssignmentProfileApplicationDto[]? assignmentProfileApplicationDtos = currentApplications
                .Select(d => new { d.Application, d.IntuneId })
                .Select(group => new AssignmentProfileApplicationDto
                {
                    SubscriptionId = group.Application.SubscriptionId,
                    IntuneId = group.IntuneId,
                    Name = group.Application.PublicApplication.Name,
                    IsPrivate = false,
                })
                .ToArray();

            if (phase.AssignmentProfile is not null)
            {
                await _assignmentProfileService.AssignAssignmentProfileToApplicationsNoCheckAsync(loggedInUser, phase.AssignmentProfile, assignmentProfileApplicationDtos, assignmentProfileGroups, generateNotifications: false);
            }

            // last phase
            if (phase.Index == phase.DeploymentSchedule.DeploymentSchedulePhases.Count - 1)
            {
                var previousApplications = phase
                    .DeploymentSchedule
                    .SubscriptionPublicApplications
                    .Where(a => applicationIds.Contains(a.PublicApplicationId))
                    .SelectMany(a => a.DeploymentScheduleApplications)
                    .Where(a => a.Type == DeploymentScheduleApplicationType.Previous)
                    .ToArray();

                foreach (var application in previousApplications)
                {
                    await _deployIntunewinService.RemoveDeployedApplicationAsync(subscriptionId, application.IntuneId);
                }

                _applicationDbContext.DeploymentScheduleSubscriptionPublicApplications.RemoveRange(previousApplications);

                await _applicationDbContext.SaveChangesAsync();

                foreach (var application in currentApplications)
                {
                    var shouldTriggerAgain = phase.DeploymentSchedule.CronTrigger is null;
                    _backgroundJobService.Enqueue(() => DeployCurrentPublicApplicationAsync(loggedInUser, subscriptionId, application.ApplicationId, application.ArchiveFileName, application.Version, shouldTriggerAgain));
                }

                foreach (var application in currentApplications)
                {
                    application.Type = DeploymentScheduleApplicationType.Previous;
                    application.PhaseState = PhaseState.Finished;
                }
            }
            else
            {
                if (applicationIds.Length == 0)
                {
                    return;
                }

                var nextPhase = phase
                    .DeploymentSchedule
                    .DeploymentSchedulePhases
                    .Where(p => p.Index > phase.Index)
                    .OrderBy(p => p.Index)
                    .FirstOrDefault();

                if (nextPhase is null)
                {
                    return;
                }

                DateTimeOffset date = firstPhaseDate.AddDays(nextPhase.OffsetDays * _options.OffsetDays);

                _backgroundJobService.Schedule(() => PublicPhaseJobAsync(loggedInUser, subscriptionId, nextPhase.Id, applicationIds, firstPhaseDate), date);

                foreach (var currentApplication in currentApplications)
                {
                    currentApplication.PhaseState = PhaseState.Finished;
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task DeployCurrentPrivateApplicationAsync(UserDto loggedInUser, int applicationId, string deployingArchiveFileName, string deployingVersion, bool shouldTriggerAgain)
        {
            var application = await _applicationDbContext.PrivateApplications.FindAsync(applicationId);

            if (application is null)
            {
                return;
            }

            var latestVersion = application.Version;

            application.DeploymentStatus = DeploymentStatus.InProgress;

            await _applicationDbContext.SaveChangesAsync();
            await _deployIntunewinService.DeployPrivateApplicationVersionForSubscriptionAsync(loggedInUser, applicationId, deployingVersion, null, true);

            int versionCheck = ApplicationHelper.CompareAppVersions(
                    latestVersion,
                    application.DeployedVersion
                );

            if (versionCheck > 0)
            {
                application.DeploymentStatus = DeploymentStatus.SuccessfulNotUpToDate;

                await _applicationDbContext.SaveChangesAsync();

                if (shouldTriggerAgain)
                {
                    await TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, application.DeploymentScheduleId.Value, (applicationId, true));
                }
            }
            else
            {
                await _applicationDbContext.SaveChangesAsync();
            }
        }

        public async Task DeployCurrentPublicApplicationAsync(UserDto loggedInUser, Guid subscriptionId, int applicationId, string deployingArchiveFileName, string deployingVersion, bool shouldTriggerAgain)
        {
            var application = await _applicationDbContext.SubscriptionPublicApplications.FindAsync(subscriptionId, applicationId);

            if (application is null)
            {
                return;
            }

            var latestVersion = application.PublicApplication.Version;

            application.DeploymentStatus = DeploymentStatus.InProgress;

            await _applicationDbContext.SaveChangesAsync();
            await _deployIntunewinService.DeployPublicApplicationVersionForSubscriptionAsync(loggedInUser, subscriptionId, applicationId, deployingVersion, null, true);

            int versionCheck = ApplicationHelper.CompareAppVersions(
                   latestVersion,
                   application.DeployedVersion
               );

            if (versionCheck > 0)
            {
                application.DeploymentStatus = DeploymentStatus.SuccessfulNotUpToDate;

                await _applicationDbContext.SaveChangesAsync();

                if (shouldTriggerAgain)
                {
                    await TriggerJobAsync(loggedInUser, loggedInUser.SubscriptionId, application.DeploymentScheduleId.Value, (applicationId, false));
                }
            }
            else
            {
                await _applicationDbContext.SaveChangesAsync();
            }
        }
    }
}