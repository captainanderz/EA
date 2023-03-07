using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Utility;
using ProjectHorizon.IntuneAppBuilder;
using ProjectHorizon.IntuneAppBuilder.Builders;
using ProjectHorizon.IntuneAppBuilder.Domain;
using ProjectHorizon.IntuneAppBuilder.Services;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class IntuneConverterService : IIntuneConverterService
    {
        private readonly IAzureBlobService _azureBlobService;

        public IntuneConverterService(IAzureBlobService azureBlobService)
        {
            _azureBlobService = azureBlobService;
        }

        public async Task ConvertToIntuneFormatAsync(string archiveFilePath)
        {
            DirectoryInfo unzippedLocation = UnzipPackage(archiveFilePath);

            string toolkitFolderPath = Path.Combine(unzippedLocation.FullName, "Toolkit");
            if (!Directory.Exists(toolkitFolderPath))
            {
                throw new FileNotFoundException($"{toolkitFolderPath} does not exist.");
            }

            //we are packing the Toolkit folder through IntuneAppBuilder
            DirectoryInfo unzippedToolkitFolderLocation = new DirectoryInfo(Path.Combine(unzippedLocation.FullName, "Toolkit"));

            IServiceCollection services = new ServiceCollection();

            AddIntuneAppPackageBuilder(unzippedToolkitFolderLocation, services);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IIntuneAppPackageBuilder builder = serviceProvider.GetRequiredService<IIntuneAppPackageBuilder>();

            await BuildAsync(builder, serviceProvider.GetRequiredService<IIntuneAppPackagingService>(),
                unzippedLocation.FullName);
        }

        public async Task<string> ConvertToIntuneAndUpload(string archiveFileName, string iconFileName = null)
        {
            string? archivePath = Path.Combine(ApplicationHelper.GetMountPath(), archiveFileName);

            await ConvertToIntuneFormatAsync(archivePath);

            string? packageFolderName = Path.GetFileNameWithoutExtension(archivePath);
            string? packageFolderPath = Path.Combine(Path.GetTempPath(), packageFolderName);

            await UploadIntuneFiles(packageFolderName, packageFolderPath);

            string? configurationXmlPath = Path.Combine(packageFolderPath, DeploymentConstants.ConfigurationFileName);
            await _azureBlobService.UploadFileToPackageCacheAsync(configurationXmlPath, packageFolderName);

            string? detectApplicationPath = Path.Combine(packageFolderPath, DeploymentConstants.DetectApplicationScriptName);
            await _azureBlobService.UploadFileToPackageCacheAsync(detectApplicationPath, packageFolderName);

            try
            {
                string? requirementPath = Path.Combine(packageFolderPath, DeploymentConstants.RequirementScriptName);
                await _azureBlobService.UploadFileToPackageCacheAsync(requirementPath, packageFolderName);
            }
            catch
            {
            }

            if (iconFileName != null)
            {
                string? iconPath = Path.Combine(packageFolderPath, iconFileName);
                if (File.Exists(iconPath))
                {
                    await _azureBlobService.UploadFileToPackageCacheAsync(iconPath, packageFolderName);
                }
            }

            Directory.Delete(packageFolderPath, recursive: true);

            return packageFolderName;
        }

        #region Private methods
        private static DirectoryInfo UnzipPackage(string zipSourceFile)
        {
            string zipFileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipSourceFile);
            DirectoryInfo extractPath = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(zipFileNameWithoutExtension);
            ZipFile.ExtractToDirectory(zipSourceFile, extractPath.FullName, true);

            return extractPath;
        }

        private static void AddIntuneAppPackageBuilder(FileSystemInfo source, IServiceCollection services)
        {
            services.AddIntuneAppBuilder();

            if (!source.Exists)
            {
                throw new InvalidOperationException($"{source.FullName} does not exist.");
            }

            if (!source.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase) && source is not DirectoryInfo)
            {
                throw new InvalidOperationException($"{source} is not a supported packaging source.");
            }

            services.AddSingleton<IIntuneAppPackageBuilder>(sp =>
                ActivatorUtilities.CreateInstance<PathIntuneAppPackageBuilder>(sp, source.FullName));
        }

        private static async Task BuildAsync(
            IIntuneAppPackageBuilder builder,
            IIntuneAppPackagingService packagingService,
            string output)
        {
            //string currentDir = Environment.CurrentDirectory;
            //Environment.CurrentDirectory = output;
            try
            {
                IntuneAppPackage package = await builder.BuildAsync(null);
                package.Data.Position = 0;

                string baseFileName = DeploymentConstants.DefaultPackageName;

                File.WriteAllText($"{output}/{baseFileName}.intunewin.json", JsonConvert.SerializeObject(package, Formatting.Indented));

                await using (FileStream? fileStream = File.Open($"{output}/{baseFileName}.intunewin", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await package.Data.CopyToAsync(fileStream);
                }

                await using (FileStream? fileStream = File.Open($"{output}/{baseFileName}.portal.intunewin", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await packagingService.BuildPackageForPortalAsync(package, fileStream);
                }
            }
            finally
            {
                //Environment.CurrentDirectory = currentDir;
            }
        }

        private async Task UploadIntuneFiles(string packageFolderName, string packageFolderPath)
        {
            //upload intunewin file
            string? appIntunewinPath = Path.Combine(packageFolderPath, $"{DeploymentConstants.DefaultPackageName}.intunewin");
            await _azureBlobService.UploadFileToPackageCacheAsync(appIntunewinPath, packageFolderName);

            //upload intunewin file made for portal
            string? appIntunewinPortalPath = Path.Combine(packageFolderPath, $"{DeploymentConstants.DefaultPackageName}.portal.intunewin");
            await _azureBlobService.UploadFileToPackageCacheAsync(appIntunewinPortalPath, packageFolderName);

            //upload json configuration file generated by IntuneAppBuilder
            string? appIntunewinJsonPath = Path.Combine(packageFolderPath, $"{DeploymentConstants.DefaultPackageName}.intunewin.json");
            await _azureBlobService.UploadFileToPackageCacheAsync(appIntunewinJsonPath, packageFolderName);
        }
        #endregion
    }
}
