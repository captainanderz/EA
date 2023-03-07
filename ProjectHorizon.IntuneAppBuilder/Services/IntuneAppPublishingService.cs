using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.IntuneAppBuilder.Domain;
using ProjectHorizon.IntuneAppBuilder.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectHorizon.IntuneAppBuilder.Services
{
    /// <inheritdoc />
    public class IntuneAppPublishingService : IIntuneAppPublishingService
    {
        private readonly ILogger logger;
        private readonly IGraphServiceClient msGraphClient;

        public IntuneAppPublishingService(ILogger<IntuneAppPublishingService> logger, IGraphServiceClient msGraphClient)
        {
            this.logger = logger;
            this.msGraphClient = msGraphClient;
        }

        /// <inheritdoc />
        public async Task<MobileLobApp> PublishAsync(IntuneAppPackage package, PackageInfo packageInfo)
        {
            logger.LogInformation($"Publishing new Intune app package for {package.App.DisplayName}.");

            SetPackageInfo(package.App, packageInfo);

            MobileLobApp newMobileApp = (MobileLobApp)await msGraphClient.DeviceAppManagement.MobileApps.Request().AddAsync(package.App);

            logger.LogInformation($"Using app {newMobileApp.Id} ({newMobileApp.DisplayName}).");

            Stopwatch stopWatch = Stopwatch.StartNew();

            MobileAppContent publishedContent = null;
            try
            {
                publishedContent = await PublishContent(package, newMobileApp);
            }
            catch
            {
                await RemoveAsync(newMobileApp.Id);
                throw;
            }

            object? update = Activator.CreateInstance(package.App.GetType());
            if (update is Win32LobApp win32LobApp)
            {
                win32LobApp.CommittedContentVersion = publishedContent?.Id;
                await msGraphClient.DeviceAppManagement.MobileApps[newMobileApp.Id].Request().UpdateAsync(win32LobApp);
            }

            //close data stream
            package.Data.Close();

            logger.LogInformation($"Published Intune app package for {newMobileApp.DisplayName} in {stopWatch.ElapsedMilliseconds}ms.");

            return newMobileApp;
        }

        public async Task<MobileLobApp> PublishUpdateAsync(IntuneAppPackage package, PackageInfo packageInfo)
        {
            logger.LogInformation($"Publishing update for Intune app package {package.App.DisplayName}.");

            MobileLobApp mobileLobApp = await GetApplicationInfoAsync(package.App.Id);

            if (mobileLobApp == null)
            {
                logger.LogInformation($"App {package.App.DisplayName} does not exist - creating new app.");

                package.App.Id = null;

                return await PublishAsync(package, packageInfo);
            }

            if (package.App.ODataType.TrimStart('#') != mobileLobApp.ODataType.TrimStart('#'))
            {
                throw new NotSupportedException($"Found existing application {mobileLobApp.DisplayName}, " +
                    $"but it is of type {mobileLobApp.ODataType.TrimStart('#')} " +
                    $"and the app being deployed is of type {package.App.ODataType.TrimStart('#')} " +
                    $"- delete the existing app and try again.");
            }

            logger.LogInformation($"Using app {mobileLobApp.Id} ({mobileLobApp.DisplayName}).");

            Stopwatch stopWatch = Stopwatch.StartNew();

            MobileAppContent? publishedContent = await PublishContent(package, mobileLobApp);

            object? update = Activator.CreateInstance(package.App.GetType());
            if (update is Win32LobApp win32LobApp)
            {
                win32LobApp.CommittedContentVersion = publishedContent?.Id;
                win32LobApp.DisplayVersion = packageInfo.PackageConfigurationContent.Version;

                SetDetectionAndIcon(win32LobApp, packageInfo);

                await msGraphClient.DeviceAppManagement.MobileApps[mobileLobApp.Id].Request().UpdateAsync(win32LobApp);
            }

            //close data stream
            package.Data.Close();

            logger.LogInformation($"Published Intune app package for {mobileLobApp.DisplayName} in {stopWatch.ElapsedMilliseconds}ms.");

            return mobileLobApp;
        }

        public async Task RemoveAsync(string intuneAppId)
        {
            try
            {
                await msGraphClient.DeviceAppManagement.MobileApps[intuneAppId].Request().DeleteAsync();
            }
            catch
            {

            }
        }

        public async Task<int> GetManagedDevicesCountAsync()
        {
            IDeviceManagementManagedDevicesCollectionPage? devices = await msGraphClient.DeviceManagement.ManagedDevices.Request().GetAsync();

            return devices.Count(d => d.OperatingSystem == "Windows");
        }

        public async Task<int> GetComanagedDevicesCountAsync()
        {
            var summary = await msGraphClient.DeviceManagement.GetComanagedDevicesSummary().Request().GetAsync();

            return summary.TotalComanagedCount ?? 0;
        }

        public async Task<MobileLobApp> GetApplicationInfoAsync(string intuneAppId)
        {
            MobileLobApp result = null;
            try
            {
                if (Guid.TryParse(intuneAppId, out Guid _))
                {
                    result = await msGraphClient.DeviceAppManagement.MobileApps[intuneAppId].Request().GetAsync()
                        as MobileLobApp ?? throw new ArgumentException($"App {intuneAppId} should be a {nameof(MobileLobApp)}.");
                }
            }
            catch (ServiceException serviceException)
            {
                if (serviceException.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            return result;
        }

        private async Task<MobileAppContent> PublishContent(IntuneAppPackage package, MobileLobApp mobileLobApp)
        {
            MobileLobAppRequestBuilder requestBuilder = new MobileLobAppRequestBuilder(msGraphClient.DeviceAppManagement
                .MobileApps[mobileLobApp.Id]
                .AppendSegmentToRequestUrl(mobileLobApp.ODataType
                .TrimStart('#')), msGraphClient);

            MobileAppContent content = null;

            // if content has never been committed, need to use last created content if one exists, otherwise an error is thrown
            if (mobileLobApp.CommittedContentVersion == null)
            {
                content = (await requestBuilder.ContentVersions.Request().OrderBy("id desc").GetAsync()).FirstOrDefault();
            }

            if (content == null)
            {
                content = await requestBuilder.ContentVersions.Request().AddAsync(new MobileAppContent());
            }
            else if ((await requestBuilder.ContentVersions[content.Id].Files.Request().Filter("isCommitted ne true").GetAsync()).Any())
            {
                // partially committed content - delete that content version
                await requestBuilder.ContentVersions[content.Id].Request().DeleteAsync();
            }

            // manifests are only supported if the app is a WindowsMobileMSI (not a Win32 app installing an msi)
            if (mobileLobApp is not WindowsMobileMSI)
            {
                package.File.Manifest = null;
            }

            await CreateAppContentFileAsync(requestBuilder.ContentVersions[content.Id], package);

            return content;
        }

        private void SetPackageInfo(MobileLobApp app, PackageInfo packageInfo)
        {
            if (packageInfo == null)
            {
                SetDefaults(app);
                return;
            }

            PackageConfigurationContent packageConfigurationContent = packageInfo.PackageConfigurationContent;
            if (app is Win32LobApp win32LobApp)
            {
                //set default return codes
                List<Win32LobAppReturnCode>? returnCodes = new List<Win32LobAppReturnCode>
                    {
                        new() { ReturnCode = 0, Type = Win32LobAppReturnCodeType.Success },
                        new() { ReturnCode = 1707, Type = Win32LobAppReturnCodeType.Success },
                        new() { ReturnCode = 3010, Type = Win32LobAppReturnCodeType.SoftReboot },
                        new() { ReturnCode = 1641, Type = Win32LobAppReturnCodeType.HardReboot },
                        new() { ReturnCode = 1618, Type = Win32LobAppReturnCodeType.Retry }
                    };

                win32LobApp.InstallExperience = new()
                {
                    RunAsAccount = (packageConfigurationContent.InstallExperience.Equals("system") ?
                    RunAsAccountType.System :
                    RunAsAccountType.User)
                };

                string architecture = string.IsNullOrWhiteSpace(packageConfigurationContent.Architecture) ?
                        "x86" :
                        packageConfigurationContent.Architecture;

                win32LobApp.InstallCommandLine = packageConfigurationContent.InstallCmd;
                win32LobApp.UninstallCommandLine = packageConfigurationContent.UninstallCmd;
                win32LobApp.Description = packageConfigurationContent.Description;
                win32LobApp.Developer = packageConfigurationContent.Developer;
                win32LobApp.DisplayName = packageConfigurationContent.Name + " (" + architecture + ")";
                win32LobApp.FileName = packageConfigurationContent.Name;
                win32LobApp.IsFeatured = packageConfigurationContent.IsFeatured;
                win32LobApp.Publisher = packageConfigurationContent.Publisher;
                win32LobApp.DisplayVersion = packageConfigurationContent.Version;
                win32LobApp.Notes = packageConfigurationContent.Notes;
                win32LobApp.Owner = packageConfigurationContent.Owner;
                win32LobApp.PrivacyInformationUrl = packageConfigurationContent.PrivacyUrl;
                win32LobApp.InformationUrl = packageConfigurationContent.InformationUrl;

                win32LobApp.ReturnCodes = returnCodes;

                SetDetectionAndIcon(win32LobApp, packageInfo);
            }
        }

        private void SetDetectionAndIcon(Win32LobApp win32LobApp, PackageInfo packageInfo)
        {
            if (packageInfo == null || win32LobApp == null)
            {
                return;
            }

            PackageConfigurationContent packageConfigurationContent = packageInfo.PackageConfigurationContent;

            //add the detection script content as a detection rule
            List<Win32LobAppRule> detectionRules = new List<Win32LobAppRule>
            {
                new Win32LobAppPowerShellScriptRule { EnforceSignatureCheck = false, RunAs32Bit = false, ScriptContent = packageInfo.DetectApplicationPowerShellScriptContent }
            };

            win32LobApp.Rules = detectionRules;

            if (packageConfigurationContent.IconContent != null)
            {
                win32LobApp.LargeIcon = new MimeContent()
                {
                    Value = packageConfigurationContent.IconContent
                };
            }
        }

        /// <summary>
        /// Gets a copy of the app with default values for null properties that are required.
        /// </summary>
        /// <param name="app"></param>
        private static void SetDefaults(MobileLobApp app)
        {
            if (app is Win32LobApp win32)
            {
                // set required properties with default values if not already specified - can be changed later in the portal
                win32.InstallExperience ??= new Win32LobAppInstallExperience { RunAsAccount = RunAsAccountType.System };
                win32.InstallCommandLine ??= win32.MsiInformation == null ? win32.SetupFilePath : $"msiexec /i \"{win32.SetupFilePath}\"";
                win32.UninstallCommandLine ??= win32.MsiInformation == null ? "echo Not Supported" : $"msiexec /x \"{win32.MsiInformation.ProductCode}\"";
                win32.Publisher ??= "-";
                win32.ApplicableArchitectures = WindowsArchitecture.X86 | WindowsArchitecture.X64;
                if (win32.Rules == null)
                {
                    if (win32.MsiInformation == null)
                    {
                        // no way to infer - use empty PS script
                        win32.Rules = new[]
                        {
                            new Win32LobAppPowerShellScriptRule
                            {
                                ScriptContent = Convert.ToBase64String(new byte[0])
                            }
                        };
                    }
                    else
                    {
                        win32.Rules = new[]
                        {
                            new Win32LobAppProductCodeRule
                            {
                                ProductCode = win32.MsiInformation.ProductCode
                            }
                        };
                    }
                }
            }
        }

        private async Task CreateAppContentFileAsync(IMobileAppContentRequestBuilder requestBuilder, IntuneAppPackage package)
        {
            // add content file
            MobileAppContentFile contentFile = await AddContentFileAsync(requestBuilder, package);

            // waits for the desired status, refreshing the file along the way
            async Task WaitForStateAsync(MobileAppContentFileUploadState state)
            {
                logger.LogInformation($"Waiting for app content file to have a state of {state}.");

                // ReSharper disable AccessToModifiedClosure - intended

                Stopwatch? waitStopwatch = Stopwatch.StartNew();

                while (true)
                {
                    contentFile = await requestBuilder.Files[contentFile.Id].Request().GetAsync();

                    if (contentFile.UploadState == state)
                    {
                        logger.LogInformation($"Waited {waitStopwatch.ElapsedMilliseconds}ms for app content file to have a state of {state}.");
                        return;
                    }

                    MobileAppContentFileUploadState[]? failedStates = new[]
                    {
                        MobileAppContentFileUploadState.AzureStorageUriRequestFailed,
                        MobileAppContentFileUploadState.AzureStorageUriRenewalFailed,
                        MobileAppContentFileUploadState.CommitFileFailed
                    };

                    if (failedStates.Contains(contentFile.UploadState.GetValueOrDefault()))
                    {
                        throw new InvalidOperationException($"{nameof(contentFile.UploadState)} is in a failed state of {contentFile.UploadState} - was waiting for {state}.");
                    }

                    const int waitTimeout = 240000;
                    const int testInterval = 2000;
                    if (waitStopwatch.ElapsedMilliseconds > waitTimeout)
                    {
                        throw new InvalidOperationException($"Timed out waiting for {nameof(contentFile.UploadState)} of {state} - current state is {contentFile.UploadState}.");
                    }

                    await Task.Delay(testInterval);
                }
                // ReSharper restore AccessToModifiedClosure
            }

            // refetch until we can get the uri to upload to
            await WaitForStateAsync(MobileAppContentFileUploadState.AzureStorageUriRequestSuccess);

            Stopwatch? sw = Stopwatch.StartNew();

            await CreateBlobAsync(package, contentFile);

            logger.LogInformation($"Uploaded app content file in {sw.ElapsedMilliseconds}ms.");

            // commit
            await requestBuilder.Files[contentFile.Id].Commit(package.EncryptionInfo).Request().PostAsync();

            // refetch until has committed
            await WaitForStateAsync(MobileAppContentFileUploadState.CommitFileSuccess);
        }

        private async Task CreateBlobAsync(IntuneAppPackage package, MobileAppContentFile contentFile)
        {
            int blockCount = 0;
            List<string>? blockIds = new List<string>();

            const int chunkSize = 5 * 1024 * 1024;
            package.Data.Seek(0, SeekOrigin.Begin);
            string? lastBlockId = (Math.Ceiling((double)package.Data.Length / chunkSize) - 1).ToString("0000");
            foreach (byte[]? chunk in Chunk(package.Data, chunkSize, false))
            {
                string? blockId = blockCount++.ToString("0000");
                logger.LogInformation($"Uploading block {blockId} of {lastBlockId} to {contentFile.AzureStorageUri}.");

                await using (MemoryStream? ms = new MemoryStream(chunk))
                {
                    await TryPutBlockAsync(contentFile, blockId, ms);
                }

                blockIds.Add(blockId);
            }

            await new CloudBlockBlob(new Uri(contentFile.AzureStorageUri)).PutBlockListAsync(blockIds);
        }

        private async Task TryPutBlockAsync(MobileAppContentFile contentFile, string blockId, Stream stream)
        {
            int attemptCount = 0;
            long position = stream.Position;
            while (true)
            {
                try
                {
                    await new CloudBlockBlob(new Uri(contentFile.AzureStorageUri)).PutBlockAsync(blockId, stream, null);
                    break;
                }
                catch (StorageException ex)
                {
                    if (!new[] { 307, 403, 400 }.Contains(ex.RequestInformation.HttpStatusCode) || attemptCount++ > 50)
                    {
                        throw;
                    }

                    logger.LogInformation($"Encountered retryable error ({ex.RequestInformation.HttpStatusCode}) uploading blob to {contentFile.AzureStorageUri} - will retry in 10 seconds.");
                    stream.Position = position;
                    await Task.Delay(10000);
                }
            }
        }

        private async Task<MobileAppContentFile> AddContentFileAsync(IMobileAppContentRequestBuilder requestBuilder, IntuneAppPackage package)
        {
            return await requestBuilder.Files.Request()
                .WithMaxRetry(10)
                .WithRetryDelay(30)
                .WithShouldRetry((delay, count, r) => r.StatusCode == HttpStatusCode.NotFound)
                .AddAsync(package.File);
        }

        /// <summary>
        /// Chunks a stream into buffers.
        /// </summary>
        private static IEnumerable<byte[]> Chunk(Stream source, int chunkSize, bool disposeSourceStream = true)
        {
            byte[]? buffer = new byte[chunkSize];

            try
            {
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[]? chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, chunk.Length);
                    yield return chunk;
                }
            }
            finally
            {
                if (disposeSourceStream)
                {
                    source.Dispose();
                }
            }
        }
    }
}
