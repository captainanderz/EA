using AutoMapper;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using ProjectHorizon.IntuneAppBuilder;
using ProjectHorizon.IntuneAppBuilder.Domain;
using ProjectHorizon.IntuneAppBuilder.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Subscription = ProjectHorizon.ApplicationCore.Entities.Subscription;

namespace ProjectHorizon.Infrastructure.Services
{
    public class DeployIntunewinService : IDeployIntunewinService
    {
        private readonly IAzureBlobService _azureBlobService;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IGraphConfigService _graphConfigService;
        private readonly INotificationService _notificationService;
        private readonly IIntuneConverterService _intuneConverterService;
        private readonly IHubContext<SignalRHub> _messageHubContext;
        private readonly IAssignmentProfileService _assignmentProfileService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly IMapper _mapper;

        private const string privateRepoAppNotes = "{0} - Private application added through Endpoint Admin";
        private const int numberOfDeployAttempts = 5;

        public DeployIntunewinService(
            IApplicationDbContext applicationDbContext,
            IAzureBlobService azureBlobService,
            IGraphConfigService graphConfigService,
            INotificationService notificationService,
            IIntuneConverterService intuneConverterService,
            IHubContext<SignalRHub> messageHubContext,
            IAssignmentProfileService assignmentProfileService,
            ILoggedInUserProvider loggedInUserProvider, 
            IMapper mapper)
        {
            _azureBlobService = azureBlobService;
            _applicationDbContext = applicationDbContext;
            _graphConfigService = graphConfigService;
            _notificationService = notificationService;
            _intuneConverterService = intuneConverterService;
            _messageHubContext = messageHubContext;
            _assignmentProfileService = assignmentProfileService;
            _loggedInUserProvider = loggedInUserProvider;
            _mapper = mapper;
        }

        [AutomaticRetry(Attempts = numberOfDeployAttempts)]
        public async Task DeployPublicApplicationForSubscriptionAsync(UserDto loggedInUser, Guid subscriptionId, int applicationId, PerformContext context, bool readRequirementScript)
        {
            PublicApplication? publicApplication = await _applicationDbContext
                .PublicApplications
                .SingleOrDefaultAsync(x => x.Id == applicationId);

            if (publicApplication == null)
            {
                await _notificationService.GenerateFailedDeploymentNotificationsAsync(
                    publicApplication,
                    publicApplication.Version,
                    subscriptionId,
                    isForPrivateRepository: false,
                    extraErrorMessage: "Public application has been removed!",
                    authorId: loggedInUser.Id);

                return;
            }

            SubscriptionPublicApplication? existingSubPubApp = await GetSubscriptionPublicApplication(applicationId, subscriptionId);

            existingSubPubApp.DeploymentStatus = DeploymentStatus.InProgress;

            await _applicationDbContext.SaveChangesAsync();

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(subscriptionId);
            }
            catch
            {
                await RecordDeployFailureForPublicApp(loggedInUser, subscriptionId, publicApplication, publicApplication.Version, "Invalid or missing Microsoft Endpoint Manager integration.");
                return;
            }

            if (string.IsNullOrEmpty(publicApplication.PackageCacheFolderName))
            {
                string archiveFullPath = await _azureBlobService.DownloadPublicRepositoryPackageAsync(publicApplication.PublicRepositoryArchiveFileName);

                string? cacheName = await _intuneConverterService.ConvertToIntuneAndUpload(archiveFullPath);

                publicApplication.PackageCacheFolderName = cacheName;
            }

            await DeployPublicApplicationVersionForSubscriptionAsync(loggedInUser, subscriptionId, publicApplication, publicApplication.Version, context, readRequirementScript);
        }

        [AutomaticRetry(Attempts = numberOfDeployAttempts)]
        public async Task DeployPublicApplicationVersionForSubscriptionAsync(UserDto loggedInUser, Guid subscriptionId, int applicationId, string version, PerformContext context, bool readRequirementScript)
        {
            PublicApplication? publicApplication = await _applicationDbContext
                .PublicApplications
                .SingleOrDefaultAsync(x => x.Id == applicationId);

            if (publicApplication == null)
            {
                await _notificationService.GenerateFailedDeploymentNotificationsAsync(
                    publicApplication,
                    publicApplication.Version,
                    subscriptionId,
                    isForPrivateRepository: false,
                    extraErrorMessage: "Public application has been removed!",
                    authorId: loggedInUser.Id);

                return;
            }

            SubscriptionPublicApplication? existingSubPubApp = await GetSubscriptionPublicApplication(applicationId, subscriptionId);

            existingSubPubApp.DeploymentStatus = DeploymentStatus.InProgress;

            await _applicationDbContext.SaveChangesAsync();

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(subscriptionId);
            }
            catch
            {
                await RecordDeployFailureForPublicApp(loggedInUser, subscriptionId, publicApplication, version, "Invalid or missing Microsoft Endpoint Manager integration.");
                return;
            }

            await DeployPublicApplicationVersionForSubscriptionAsync(loggedInUser, subscriptionId, publicApplication, version, context, readRequirementScript);
        }

        [AutomaticRetry(Attempts = numberOfDeployAttempts)]
        public async Task DeployPrivateApplicationForSubscriptionAsync(UserDto loggedInUser, int applicationId, PerformContext context, bool readRequirementScript)
        {
            PrivateApplication? privateApplication = await _applicationDbContext
                .PrivateApplications
                .SingleOrDefaultAsync(x => x.Id == applicationId);

            await DeployPrivateApplicationVersionForSubscriptionAsync(loggedInUser, privateApplication, privateApplication.Version, context, readRequirementScript);
        }

        [AutomaticRetry(Attempts = numberOfDeployAttempts)]
        public async Task DeployPrivateApplicationVersionForSubscriptionAsync(UserDto loggedInUser, int applicationId, string version, PerformContext context, bool readRequirementScript)
        {
            PrivateApplication? privateApplication = await _applicationDbContext
                .PrivateApplications
                .SingleOrDefaultAsync(x => x.Id == applicationId);

            await DeployPrivateApplicationVersionForSubscriptionAsync(loggedInUser, privateApplication, version, context, readRequirementScript);
        }

        private async Task DeployPrivateApplicationVersionForSubscriptionAsync(UserDto loggedInUser, PrivateApplication privateApplication, string version, PerformContext context, bool readRequirementScript) 
        {
            Guid subscriptionId = loggedInUser.SubscriptionId;
            int jobRetryCount = -1;

            if (context != null)
            {
                jobRetryCount = context.GetJobParameter<int>("RetryCount");
            }

            if (privateApplication == null)
            {
                await _notificationService.GenerateFailedDeploymentNotificationsAsync(
                    null,
                    string.Empty,
                    subscriptionId,
                    isForPrivateRepository: true,
                    extraErrorMessage: "Private application has been removed!",
                    authorId: loggedInUser.Id);

                await _applicationDbContext.SaveChangesAsync();
                return;
            }

            privateApplication.DeploymentStatus = DeploymentStatus.InProgress;

            await _applicationDbContext.SaveChangesAsync();

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(subscriptionId);
            }
            catch
            {
                await RecordDeployFailureForPrivateApp(loggedInUser, subscriptionId, privateApplication, version, "Invalid or missing Microsoft Endpoint Manager integration.");
                return;
            }

            if (string.IsNullOrEmpty(privateApplication.PrivateRepositoryArchiveFileName))
            {
                await RecordDeployFailureForPrivateApp(loggedInUser, subscriptionId, privateApplication, version, "No private repository archive file name found in database.");
                return;
            }

            var dto = _mapper.Map<PrivateApplicationDto>(privateApplication);
            dto.Version = version;

            var archiveFileName = ApplicationHelper.GetArchiveFileNameFromInfo(dto, subscriptionId);

            string archiveFullPath = await _azureBlobService
                .DownloadPrivateRepositoryPackageAsync(archiveFileName, subscriptionId);

            Response<ApplicationDto> response = await ApplicationHelper.ValidateAndGetInfo(archiveFullPath);

            string? packageFolderName = Path.GetFileNameWithoutExtension(archiveFullPath);
            string? packageFolderPath = Path.Combine(Path.GetTempPath(), packageFolderName);

            if (!response.IsSuccessful)
            {
                await RecordDeployFailureForPrivateApp(loggedInUser, subscriptionId, privateApplication, version, response.ErrorMessage);
                return;
            }

            try
            {
                await _intuneConverterService.ConvertToIntuneFormatAsync(archiveFullPath);

                PackageInfo packageInfo = GetPublishInformation(packageFolderPath, readRequirementScript);

                string? deploymentStatus = await PublishPrivateAppToIntuneAsync(
                    packageFolderPath,
                    subscriptionId,
                    packageInfo,
                    privateApplication);

                if (deploymentStatus == DeploymentStatus.SuccessfulUpToDate)
                {
                    await _notificationService.GenerateSuccessfulDeploymentNotificationsAsync(
                        privateApplication,
                        version,
                        subscriptionId,
                        isForPrivateRepository: true,
                        authorId: loggedInUser.Id);

                    if (privateApplication.AssignmentProfileId is not null)
                    {
                        await _assignmentProfileService.AssignAssignmentProfileToPrivateApplicationsJobAsync(loggedInUser, loggedInUser.SubscriptionId, privateApplication.AssignmentProfileId.Value, new[] { privateApplication.Id });
                    }
                }
                else
                {
                    throw new Exception("Deployment failed.");
                }
            }
            catch
            {
                if (jobRetryCount == numberOfDeployAttempts)
                {
                    await RecordDeployFailureForPrivateApp(loggedInUser, subscriptionId, privateApplication, version,
                        $"This was the last deploy attempt out of {numberOfDeployAttempts} attempts");
                }

                throw;
            }
            finally
            {
                //after publish delete temporary folder
                System.IO.Directory.Delete(packageFolderPath, true);
                System.IO.File.Delete(archiveFullPath);
            }
        }

        private async Task RecordDeployFailureForPrivateApp(UserDto loggedInUser, Guid subscriptionId, PrivateApplication privateApplication, string version, string errorMessage)
        {
            await _notificationService.GenerateFailedDeploymentNotificationsAsync(
                privateApplication,
                version,
                subscriptionId,
                isForPrivateRepository: true,
                extraErrorMessage: errorMessage,
                authorId: loggedInUser.Id);

            privateApplication.DeploymentStatus = DeploymentStatus.Failed;
            privateApplication.DeployedVersion = privateApplication.Version;

            await _applicationDbContext.SaveChangesAsync();
        }

        private async Task RecordDeployFailureForPublicApp(UserDto loggedInUser, Guid subscriptionId, PublicApplication publicApplication, string version, string errorMessage)
        {
            SubscriptionPublicApplication? existingSubPubApp = await GetSubscriptionPublicApplication(publicApplication.Id, subscriptionId);

            existingSubPubApp.DeploymentStatus = DeploymentStatus.Failed;
            existingSubPubApp.DeployedVersion = publicApplication.Version;

            await _notificationService.GenerateFailedDeploymentNotificationsAsync(
                publicApplication,
                version,
                subscriptionId,
                isForPrivateRepository: false,
                extraErrorMessage: errorMessage,
                authorId: loggedInUser.Id);

            await _applicationDbContext.SaveChangesAsync();
        }

        private async Task DeployPublicApplicationVersionForSubscriptionAsync(UserDto loggedInUser, Guid subscriptionId, PublicApplication publicApplication, string version, PerformContext context, bool readRequirementScript)
        {
            int jobRetryCount = -1;

            if (context != null)
            {
                jobRetryCount = context.GetJobParameter<int>("RetryCount");
            }

            try
            {
                await _graphConfigService.CheckGraphStatusAsync(subscriptionId);
            }
            catch
            {
                await RecordDeployFailureForPublicApp(loggedInUser, subscriptionId, publicApplication, version, "Invalid or missing Microsoft Endpoint Manager integration.");
                return;
            }

            DirectoryInfo? tempIntuneFolder = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());
            var dto = _mapper.Map<PublicApplicationDto>(publicApplication);
            dto.Version = version;

            var packageName = ApplicationHelper.GetPackageNameFromInfo(dto);

            try
            {
                bool foundCache = await _azureBlobService
                    .DownloadPackageFromCacheAsync(packageName, tempIntuneFolder.FullName);

                if (!foundCache)
                {
                    var archiveName = ApplicationHelper.GetArchiveFileNameFromInfo(dto);
                    string archiveFullPath = await _azureBlobService.DownloadPublicRepositoryPackageAsync(archiveName);
                    string? cacheName = await _intuneConverterService.ConvertToIntuneAndUpload(archiveFullPath);
                    await _azureBlobService.DownloadPackageFromCacheAsync(cacheName, tempIntuneFolder.FullName);
                }

                string fullPathToPackage = Path.Combine(tempIntuneFolder.FullName, packageName);
                PackageInfo packageInfo = GetPublishInformation(fullPathToPackage, readRequirementScript);

                string? deploymentStatus = await PublishPublicAppToIntuneAsync(fullPathToPackage, publicApplication.Id, subscriptionId, packageInfo);


                if (deploymentStatus == DeploymentStatus.SuccessfulUpToDate)
                {
                    await _notificationService.GenerateSuccessfulDeploymentNotificationsAsync(
                        publicApplication,
                        version,
                        subscriptionId,
                        isForPrivateRepository: false,
                        authorId: loggedInUser.Id);

                    SubscriptionPublicApplication? subPubApp = await GetSubscriptionPublicApplication(publicApplication.Id, subscriptionId);

                    if (subPubApp.AssignmentProfileId is not null)
                    {
                        await _assignmentProfileService.AssignAssignmentProfileToPublicApplicationsJobAsync(loggedInUser, loggedInUser.SubscriptionId, subPubApp.AssignmentProfileId.Value, new[] { publicApplication.Id });
                    }
                }
                else
                {
                    throw new Exception("Deployment failed.");
                }

                await RemoveApprovalsAsync(publicApplication.Id, subscriptionId);
            }
            catch
            {
                if (jobRetryCount == numberOfDeployAttempts)
                {
                    await RecordDeployFailureForPublicApp(loggedInUser, subscriptionId, publicApplication, version, 
                        $"This was the last deploy attempt out of {numberOfDeployAttempts} attempts");
                }

                throw;
            }
            finally
            {
                //after publish delete temporary folder
                System.IO.Directory.Delete(tempIntuneFolder.FullName, true);
            }
        }

        public async Task RemoveDeployedPublicApplicationAsync(int applicationId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            SubscriptionPublicApplication existingSubPubApp = await GetSubscriptionPublicApplication(applicationId, loggedInUser.SubscriptionId);

            if (existingSubPubApp != null)
            {
                await RemoveDeployedApplicationAsync(loggedInUser.SubscriptionId, existingSubPubApp.IntuneId);
            }
        }

        public async Task RemoveDeployedApplicationAsync(Guid subscribtionId, string intuneId)
        {
            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscribtionId);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();
            await intuneAppPublishingService.RemoveAsync(intuneId);
        }

        public async Task UpdateDevicesCountAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            var subscription = await _applicationDbContext.Subscriptions.FindAsync(loggedInUser.SubscriptionId);

            await UpdateDevicesCountForSubscriptionAsync(subscription);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task UpdateDevicesCountForAllSubscriptionsAsync()
        {
            var subscriptions = await _applicationDbContext
                .Subscriptions
                .ToListAsync();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await UpdateDevicesCountForSubscriptionAsync(subscription);
                }
                catch (InvalidOperationException) { }
                catch
                {
                    // wait 21s before trying again (deviceManagement limit per app per tenant is 1000 requests per 20s)
                    await Task.Delay(21000);

                    try
                    {
                        await UpdateDevicesCountForSubscriptionAsync(subscription);
                    }
                    catch { }
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<MobileLobApp> GetDeployedPublicApplicationInfoAsync(int applicationId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            SubscriptionPublicApplication existingSubPubApp = await GetSubscriptionPublicApplication(applicationId, loggedInUser.SubscriptionId);

            if (existingSubPubApp != null)
            {
                ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(loggedInUser.SubscriptionId);
                IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();
                return await intuneAppPublishingService.GetApplicationInfoAsync(existingSubPubApp.IntuneId);
            }

            return null;
        }

        private static DirectoryInfo UnzipPackage(string zipSourceFile)
        {
            string zipFileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipSourceFile);
            DirectoryInfo extractPath = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(zipFileNameWithoutExtension);
            ZipFile.ExtractToDirectory(zipSourceFile, extractPath.FullName, true);

            return extractPath;
        }

        public async Task TogglePrivateRequirementScriptAsync(Guid subscriptionId, int applicationId, string intuneId, bool useRequirementScript)
        {
            PrivateApplication? application = await _applicationDbContext
                .PrivateApplications
                .SingleOrDefaultAsync(x =>
                (x.Id == applicationId) &&
                (x.SubscriptionId == subscriptionId));

            if (application is null)
            {
                return;
            }

            IAuthenticationProvider clientCredentialsAuthProvider = await GetAuthenticationProvider(subscriptionId);
            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);

            string archiveFullPath = await _azureBlobService
                .DownloadPrivateRepositoryPackageAsync(application.PrivateRepositoryArchiveFileName, subscriptionId);
            UnzipPackage(archiveFullPath);

            string? packageFolderName = Path.GetFileNameWithoutExtension(archiveFullPath);
            string? packageFolderPath = Path.Combine(Path.GetTempPath(), packageFolderName);

            PackageInfo packageInfo = GetPublishInformation(packageFolderPath);

            List<Win32LobAppRule> rules = new List<Win32LobAppRule>
            {
                new Win32LobAppPowerShellScriptRule 
                { 
                    EnforceSignatureCheck = false, 
                    RunAs32Bit = false, 
                    ScriptContent = packageInfo.DetectApplicationPowerShellScriptContent 
                }
            };

            if (useRequirementScript)
            {
                rules.Add(new Win32LobAppPowerShellScriptRule 
                { 
                    RuleType = Win32LobAppRuleType.Requirement, 
                    DisplayName = DeploymentConstants.RequirementScriptName,
                    EnforceSignatureCheck = false,
                    RunAs32Bit = false,
                    RunAsAccount = RunAsAccountType.System,
                    OperationType = Win32LobAppPowerShellScriptRuleOperationType.Integer,
                    Operator = Win32LobAppRuleOperator.Equal,
                    ComparisonValue = "1",
                    ScriptContent = packageInfo.RequirementPowerShellScriptContent
                });
            }

            Win32LobApp win32LobApp = new()
            {
                Rules = rules,
            };

            await graphServiceClient
                .DeviceAppManagement
                .MobileApps[intuneId]
                .Request()
                .UpdateAsync(win32LobApp);
        }

        public async Task TogglePublicRequirementScriptAsync(Guid subscriptionId, int applicationId, string intuneId, bool useRequirementScript)
        {
            SubscriptionPublicApplication? application = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(x =>
                    x.PublicApplicationId == applicationId &&
                    x.SubscriptionId == subscriptionId)
                .Include(x => x.PublicApplication)
                .FirstAsync();

            if (application is null)
            {
                return;
            }

            IAuthenticationProvider clientCredentialsAuthProvider = await GetAuthenticationProvider(subscriptionId);
            GraphServiceClient? graphServiceClient = new GraphServiceClient(clientCredentialsAuthProvider);
            DirectoryInfo? tempIntuneFolder = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());

            await _azureBlobService
                     .DownloadPackageFromCacheAsync(application.PublicApplication.PackageCacheFolderName, tempIntuneFolder.FullName);

            string fullPathToPackage = Path.Combine(tempIntuneFolder.FullName, application.PublicApplication.PackageCacheFolderName);
            PackageInfo packageInfo = GetPublishInformation(fullPathToPackage);

            List<Win32LobAppRule> rules = new List<Win32LobAppRule>
            {
                new Win32LobAppPowerShellScriptRule
                {
                    EnforceSignatureCheck = false,
                    RunAs32Bit = false,
                    ScriptContent = packageInfo.DetectApplicationPowerShellScriptContent
                }
            };

            if (useRequirementScript)
            {
                rules.Add(new Win32LobAppPowerShellScriptRule
                {
                    RuleType = Win32LobAppRuleType.Requirement,
                    DisplayName = DeploymentConstants.RequirementScriptName,
                    EnforceSignatureCheck = false,
                    RunAs32Bit = false,
                    RunAsAccount = RunAsAccountType.System,
                    OperationType = Win32LobAppPowerShellScriptRuleOperationType.Integer,
                    Operator = Win32LobAppRuleOperator.Equal,
                    ComparisonValue = "1",
                    ScriptContent = packageInfo.RequirementPowerShellScriptContent
                });
            }

            Win32LobApp win32LobApp = new()
            {
                Rules = rules,
            };

            await graphServiceClient
                .DeviceAppManagement
                .MobileApps[intuneId]
                .Request()
                .UpdateAsync(win32LobApp);
        }

        private async Task<SubscriptionPublicApplication> GetSubscriptionPublicApplication(int applicationId, Guid subscriptionId)
        {
            return await _applicationDbContext
                .SubscriptionPublicApplications
                .SingleOrDefaultAsync(x =>
                (x.PublicApplicationId == applicationId) &&
                (x.SubscriptionId == subscriptionId));
        }

        private async Task<IAuthenticationProvider> GetAuthenticationProvider(Guid subscriptionId)
        {
            IAuthenticationProvider clientCredentialsAuthProvider = null;
            if (await _graphConfigService.HasGraphConfigAsync(subscriptionId))
            {
                GraphConfigDto graphConfigDto = await _graphConfigService.GetGraphConfigAsync(subscriptionId);
                clientCredentialsAuthProvider = new GraphAuthProvider(graphConfigDto);
            }

            return clientCredentialsAuthProvider;
        }

        private static PackageInfo GetPublishInformation(string fullPathToPackage, bool readRequirementScript = true)
        {
            string xmlFileLocation = Path.Combine(fullPathToPackage, DeploymentConstants.ConfigurationFileName);
            if (!System.IO.File.Exists(xmlFileLocation))
            {
                throw new FileNotFoundException($"XML Config file not found at: {fullPathToPackage}");
            }

            string detectApplicationFileLocation = Path.Combine(fullPathToPackage, DeploymentConstants.DetectApplicationScriptName);
            if (!System.IO.File.Exists(detectApplicationFileLocation))
            {
                throw new FileNotFoundException($"Detect-Application.ps1 not found at: {fullPathToPackage}");
            }

            string detectApplicationContent = System.IO.File.ReadAllText(Path.Combine(fullPathToPackage, DeploymentConstants.DetectApplicationScriptName));
            string detectApplicationContentEncoded = Base64Encode(detectApplicationContent);

            string requirementContent = string.Empty;

            if (readRequirementScript)
            {
                requirementContent = System.IO.File.ReadAllText(Path.Combine(fullPathToPackage, DeploymentConstants.RequirementScriptName));
            }

            string requirementContentEncoded = Base64Encode(requirementContent);

            PackageConfigurationContent xmlData = ReadXmlFile<PackageConfigurationContent>(xmlFileLocation);

            //set image information
            xmlData.IconContent = GetImageInfo(Path.Combine(fullPathToPackage, xmlData.Icon));

            return new PackageInfo()
            {
                PackageConfigurationContent = xmlData,
                DetectApplicationPowerShellScriptContent = detectApplicationContentEncoded,
                RequirementPowerShellScriptContent = requirementContentEncoded
            };
        }

        private static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static byte[] GetImageInfo(string iconPath)
        {
            byte[] result = null;
            try
            {
                using System.Drawing.Image img = System.Drawing.Image.FromFile(iconPath);

                using MemoryStream memoryStream = new MemoryStream();
                img.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                result = memoryStream.ToArray();
            }
            catch (FileNotFoundException)
            {
                //if image not found, do nothing, no icon displayed in the portal
                //does not influence deployment, can be uploaded manually
            }

            return result;
        }

        public async Task<string> PublishPrivateAppToIntuneAsNewAsync(
            Guid subscriptionId,
            int applicationId)
        {
            PrivateApplication application = await _applicationDbContext
                .PrivateApplications
                .SingleAsync(x => x.Id == applicationId);

            string archiveFullPath = await _azureBlobService.DownloadPrivateRepositoryPackageAsync(application.PrivateRepositoryArchiveFileName, subscriptionId);

            await _intuneConverterService.ConvertToIntuneFormatAsync(archiveFullPath);

            string? packageFolderName = Path.GetFileNameWithoutExtension(archiveFullPath);
            string? packageFolderPath = Path.Combine(Path.GetTempPath(), packageFolderName);

            PackageInfo packageInfo = GetPublishInformation(packageFolderPath);
            string intuneJsonFile = Path.Combine(packageFolderPath, $"{DeploymentConstants.DefaultPackageName}.intunewin.json");
            FileInfo intuneJsonFileInfo = new FileInfo(intuneJsonFile);

            IntuneAppPackage intuneApplicationPackage = await ReadPackageAsync(intuneJsonFileInfo);
            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscriptionId);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();

            MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishAsync(intuneApplicationPackage, packageInfo);

            return mobileLobApp.Id;
        }

        public async Task<string> PublishPublicAppToIntuneAsNewAsync(
            Guid subscriptionId,
            int applicationId)
        {
            SubscriptionPublicApplication subPubApp = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(x =>
                    x.PublicApplicationId == applicationId &&
                    x.SubscriptionId == subscriptionId)
                .Include(x => x.PublicApplication)
                .FirstAsync();

            DirectoryInfo? tempIntuneFolder = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());
            string fullPathToPackage = Path.Combine(tempIntuneFolder.FullName, subPubApp.PublicApplication.PackageCacheFolderName);
            string intuneJsonFile = Path.Combine(fullPathToPackage, $"{DeploymentConstants.DefaultPackageName}.intunewin.json");

            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscriptionId);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();

            try
            {
                await _azureBlobService
                    .DownloadPackageFromCacheAsync(subPubApp.PublicApplication.PackageCacheFolderName, tempIntuneFolder.FullName);

                PackageInfo packageInfo = GetPublishInformation(fullPathToPackage);
                FileInfo intuneJsonFileInfo = new FileInfo(intuneJsonFile);
                IntuneAppPackage intuneApplicationPackage = await ReadPackageAsync(intuneJsonFileInfo);

                MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishAsync(intuneApplicationPackage, packageInfo);

                return mobileLobApp.Id;
            }
            finally
            {
                System.IO.Directory.Delete(tempIntuneFolder.FullName, true);
            }

        }

        private async Task<string> PublishPublicAppToIntuneAsync(
            string fullPathToPackage,
            int applicationId,
            Guid subscriptionId,
            PackageInfo packageInfo)
        {
            string intuneJsonFile = Path.Combine(fullPathToPackage, $"{DeploymentConstants.DefaultPackageName}.intunewin.json");
            FileInfo intuneJsonFileInfo = new FileInfo(intuneJsonFile);

            IntuneAppPackage intuneApplicationPackage = await ReadPackageAsync(intuneJsonFileInfo);

            SubscriptionPublicApplication existingSubPubApp = await GetSubscriptionPublicApplication(applicationId, subscriptionId);

            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscriptionId);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();

            string status = DeploymentStatus.InProgress;
            if (existingSubPubApp != null && !string.IsNullOrEmpty(existingSubPubApp.IntuneId))
            {
                try
                {
                    intuneApplicationPackage.App.Id = existingSubPubApp.IntuneId;

                    MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishUpdateAsync(intuneApplicationPackage, packageInfo);

                    if (mobileLobApp != null)
                    {
                        existingSubPubApp.IntuneId = mobileLobApp.Id;
                        existingSubPubApp.DeployedVersion = packageInfo.PackageConfigurationContent.Version;

                        int versionCheck = ApplicationHelper.CompareAppVersions(existingSubPubApp.PublicApplication.Version, existingSubPubApp.DeployedVersion);
                        status = versionCheck > 0 ? DeploymentStatus.SuccessfulNotUpToDate : DeploymentStatus.SuccessfulUpToDate;
                    }
                }
                finally
                {
                    existingSubPubApp.DeploymentStatus = status;

                    await _applicationDbContext.SaveChangesAsync();
                }
            }
            else
            {
                SubscriptionPublicApplication subPubApp = existingSubPubApp;

                try
                {
                    MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishAsync(intuneApplicationPackage, packageInfo);

                    if (mobileLobApp != null)
                    {
                        subPubApp.IntuneId = mobileLobApp.Id;
                        subPubApp.DeployedVersion = packageInfo.PackageConfigurationContent.Version;

                        int versionCheck = ApplicationHelper.CompareAppVersions(subPubApp.PublicApplication.Version, subPubApp.DeployedVersion);
                        status = versionCheck > 0 ? DeploymentStatus.SuccessfulNotUpToDate : DeploymentStatus.SuccessfulUpToDate;
                    }

                    if (existingSubPubApp == null)
                    {
                        _applicationDbContext.SubscriptionPublicApplications.Add(subPubApp);
                    }
                }
                finally
                {
                    subPubApp.DeploymentStatus = status;
                    await _applicationDbContext.SaveChangesAsync();
                }
            }

            return status;
        }

        private async Task<string> PublishPrivateAppToIntuneAsync(
            string fullPathToPackage,
            Guid subscriptionId,
            PackageInfo packageInfo,
            PrivateApplication privateApplication)
        {
            string intuneJsonFile = Path.Combine(fullPathToPackage, $"{DeploymentConstants.DefaultPackageName}.intunewin.json");
            FileInfo intuneJsonFileInfo = new FileInfo(intuneJsonFile);

            IntuneAppPackage intuneApplicationPackage = await ReadPackageAsync(intuneJsonFileInfo);
            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscriptionId);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();

            string status = DeploymentStatus.InProgress;
            if (!string.IsNullOrEmpty(privateApplication.IntuneId))
            {
                try
                {
                    intuneApplicationPackage.App.Id = privateApplication.IntuneId;

                    MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishUpdateAsync(intuneApplicationPackage, packageInfo);

                    if (mobileLobApp != null)
                    {
                        privateApplication.IntuneId = mobileLobApp.Id;
                        privateApplication.DeployedVersion = packageInfo.PackageConfigurationContent.Version;

                        int versionCheck = ApplicationHelper.CompareAppVersions(privateApplication.Version, privateApplication.DeployedVersion);
                        status = versionCheck > 0 ? DeploymentStatus.SuccessfulNotUpToDate : DeploymentStatus.SuccessfulUpToDate;
                    }
                }
                finally
                {
                    privateApplication.DeploymentStatus = status;

                    await _applicationDbContext.SaveChangesAsync();
                }
            }
            else
            {
                try
                {
                    packageInfo.PackageConfigurationContent.Notes = string.Format(privateRepoAppNotes, packageInfo.PackageConfigurationContent.Name);

                    MobileLobApp mobileLobApp = await intuneAppPublishingService.PublishAsync(intuneApplicationPackage, packageInfo);

                    if (mobileLobApp != null)
                    {
                        status = DeploymentStatus.SuccessfulUpToDate;
                        privateApplication.IntuneId = mobileLobApp.Id;
                        privateApplication.DeployedVersion = packageInfo.PackageConfigurationContent.Version;

                        int versionCheck = ApplicationHelper.CompareAppVersions(privateApplication.Version, privateApplication.DeployedVersion);
                        status = versionCheck > 0 ? DeploymentStatus.SuccessfulNotUpToDate : DeploymentStatus.SuccessfulUpToDate;
                    }
                }
                finally
                {
                    privateApplication.DeploymentStatus = status;

                    await _applicationDbContext.SaveChangesAsync();
                }
            }

            return status;
        }

        private static T ReadXmlFile<T>(string configFile) where T : class
        {
            string xmlContent = System.IO.File.ReadAllText(configFile);

            using TextReader reader = new StringReader(xmlContent.Trim());
            T returnedObject = (T)new XmlSerializer(typeof(T)).Deserialize(reader);

            return returnedObject;
        }

        private static async Task<IntuneAppPackage> ReadPackageAsync(FileInfo file)
        {
            IntuneAppPackage package = JsonConvert.DeserializeObject<IntuneAppPackage>(await System.IO.File.ReadAllTextAsync(file.FullName));
            string? dataPath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName));

            if (!System.IO.File.Exists(dataPath))
            {
                throw new FileNotFoundException($"Could not find data file at {dataPath}.");
            }

            package.Data = System.IO.File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return package;
        }

        private async Task<ServiceProvider> GetIntunePublishingServiceAsync(Guid subscriptionId)
        {
            IAuthenticationProvider clientCredentialsAuthProvider = await GetAuthenticationProvider(subscriptionId);
            if (clientCredentialsAuthProvider == null)
            {
                throw new InvalidOperationException($"Authentication provider not available");
            }

            //intune publish service
            ServiceCollection services = new ServiceCollection();
            services.AddIntuneAppBuilder();
            services.AddLogging(builder =>
            {
                // don't write info for HttpClient
                builder.AddFilter((category, level) => category.StartsWith("System.Net.Http.HttpClient") ?
                level >= LogLevel.Warning :
                level >= LogLevel.Information);
                builder.AddConsole();
            });

            if (clientCredentialsAuthProvider != null)
            {
                services.TryAddSingleton<IGraphServiceClient>(sp => new GraphServiceClient(clientCredentialsAuthProvider));
            }

            return services.BuildServiceProvider();
        }

        private async Task RemoveApprovalsAsync(int publicApplicationId, Guid subscriptionId)
        {
            _applicationDbContext.Approvals
               .Where(a =>
                    a.IsActive &&
                    a.SubscriptionId == subscriptionId &&
                    a.PublicApplicationId == publicApplicationId)
               .ToList()
               .ForEach(a => a.IsActive = false);

            await _applicationDbContext.SaveChangesAsync();

            var subscriptionUserIds = await _applicationDbContext.SubscriptionUsers
                    .Where(subscriptionUser => subscriptionUser.SubscriptionId == subscriptionId)
                    .Select(subscriptionUser => subscriptionUser.ApplicationUserId)
                    .ToArrayAsync();

            await _messageHubContext.Clients.Users(subscriptionUserIds).SendAsync(SignalRMessages.UpdateApprovalCount);
        }
        private async Task UpdateDevicesCountForSubscriptionAsync(Subscription subscription)
        {
            ServiceProvider? serviceProvider = await GetIntunePublishingServiceAsync(subscription.Id);
            IIntuneAppPublishingService intuneAppPublishingService = serviceProvider.GetRequiredService<IIntuneAppPublishingService>();
            var managedDevicesCount = await intuneAppPublishingService.GetManagedDevicesCountAsync(); ;
            var comanagedDevicesCount = await intuneAppPublishingService.GetComanagedDevicesCountAsync();
            int deviceCount = managedDevicesCount + comanagedDevicesCount;

            subscription.DeviceCount = deviceCount;
        }

        public string ToOrdinal(int value)
        {
            // Start with the most common extension.
            string extension = "th";

            // Examine the last 2 digits.
            int last_digits = value % 100;

            // If the last digits are 11, 12, or 13, use th. Otherwise:
            if (last_digits < 11 || last_digits > 13)
            {
                // Check the last digit.
                switch (last_digits % 10)
                {
                    case 1:
                        extension = "st";
                        break;
                    case 2:
                        extension = "nd";
                        break;
                    case 3:
                        extension = "rd";
                        break;
                }
            }

            return extension;
        }
    }
}
