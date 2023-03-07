using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAzureBlobService
    {
        Task UploadPublicRepositoryPackageAsync(string packageLocation);
        Task UploadFileToPackageCacheAsync(string filePath, string packageFolderName);
        Task<string> DownloadPublicRepositoryPackageAsync(string packageName);
        Task<bool> DownloadPackageFromCacheAsync(string cacheFolderName, string downloadPath);
        Task RemovePublicRepositoryPackageAsync(string packageName);

        Task UploadPackageOnPrivateRepositoryAsync(Guid subscriptionId, string packageLocation);
        Task<string> DownloadPrivateRepositoryPackageAsync(string packageName, Guid subscriptionId);

        // For more information about SAS, please check:
        // https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview
        Uri GetDownloadUriForPackageUsingSas(string packageName, Guid? subscriptionId = null);

        Task<DownloadInfoDto> DownloadProfilePictureAsync(string userId);
        Task UploadProfilePictureAsync(string userId, Stream picture);
        Task RemoveProfilePictureAsync(string userId);

        Task<DownloadInfoDto> DownloadLogoAsync(Guid subscriptionId);
        Task UploadLogoAsync(Guid subscriptionId, Stream picture);
        Task RemoveLogoAsync(Guid subscriptionId);
    }
}