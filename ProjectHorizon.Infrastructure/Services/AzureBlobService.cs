using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _publicRepositoryContainer;
        private readonly BlobContainerClient _packageCacheContainer;
        private readonly BlobContainerClient _profilePictureContainer;
        private readonly BlobContainerClient _logoContainer;
        private readonly string _privateRepoBlobContainerNameFormat = "private-repo-{0}";
        private readonly StorageSharedKeyCredential _storageSharedKeyCredential;

        public AzureBlobService(IOptions<AzureStorageInformation> options)
        {
            _blobServiceClient = new BlobServiceClient(options.Value.AzureStorageConnection);
            _publicRepositoryContainer = _blobServiceClient.GetBlobContainerClient(options.Value.PublicRepositoryName);
            _packageCacheContainer = _blobServiceClient.GetBlobContainerClient(options.Value.PackageCacheName);
            _profilePictureContainer = _blobServiceClient.GetBlobContainerClient(options.Value.ProfilePicturesName);
            _logoContainer = _blobServiceClient.GetBlobContainerClient(options.Value.LogosName);

            CloudStorageAccount? accountInfo = CloudStorageAccount.Parse(options.Value.AzureStorageConnection);
            string? accountKey = accountInfo.Credentials.ExportBase64EncodedKey();
            string? accountName = accountInfo.Credentials.AccountName;

            _storageSharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
        }

        public async Task<bool> DownloadPackageFromCacheAsync(string cacheFolderName, string downloadPath)
        {
            if (string.IsNullOrEmpty(cacheFolderName))
            {
                throw new ArgumentNullException(nameof(cacheFolderName));
            }

            if (string.IsNullOrEmpty(downloadPath))
            {
                throw new ArgumentNullException(nameof(downloadPath));
            }

            AsyncPageable<BlobItem> list = _packageCacheContainer.GetBlobsAsync(prefix: cacheFolderName);
            string fullDownloadPath = Path.Combine(downloadPath, cacheFolderName);
            Directory.CreateDirectory(fullDownloadPath);

            bool found = false;

            await foreach (BlobItem? item in list)
            {
                found = true;
                BlockBlobClient blockBlob = _packageCacheContainer.GetBlockBlobClient(item.Name);
                string fileName = item.Name.Split('/').Last();

                using FileStream? fileStream = File.OpenWrite(Path.Combine(fullDownloadPath, fileName));
                await blockBlob.DownloadToAsync(fileStream);
            }

            return found;
        }

        public Task UploadPublicRepositoryPackageAsync(string packageLocation)
        {
            if (string.IsNullOrEmpty(packageLocation))
            {
                throw new ArgumentNullException(nameof(packageLocation));
            }

            return UploadPackageInternalAsync(packageLocation);
        }

        private async Task UploadPackageInternalAsync(string packageLocation)
        {
            string zipName = Path.GetFileName(packageLocation);
            BlobClient? blobClient = _publicRepositoryContainer.GetBlobClient(zipName);

            using FileStream? uploadFileStream = File.OpenRead(packageLocation);
            await blobClient.UploadAsync(uploadFileStream, true);
        }

        public Task UploadFileToPackageCacheAsync(string filePath, string packageFolderName)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(packageFolderName))
            {
                throw new ArgumentNullException(nameof(packageFolderName));
            }

            return UploadFileToPackageCacheInternalAsync(filePath, packageFolderName);
        }

        private async Task UploadFileToPackageCacheInternalAsync(string filePath, string packageFolderName)
        {
            string fileName = Path.GetFileName(filePath);
            BlobClient blobClient = _packageCacheContainer.GetBlobClient(Path.Combine(packageFolderName, fileName));

            using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, true);
        }

        public Task RemovePublicRepositoryPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            return RemovePackageInternalAsync(packageName);
        }

        private async Task RemovePackageInternalAsync(string packageName)
        {
            BlobClient blobClient = _publicRepositoryContainer.GetBlobClient(packageName);
            await blobClient.DeleteAsync();
        }

        public async Task UploadPackageOnPrivateRepositoryAsync(Guid subscriptionId, string packageLocation)
        {
            if (string.IsNullOrEmpty(packageLocation))
            {
                throw new ArgumentNullException(nameof(packageLocation));
            }

            if (subscriptionId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            string? blobContainerName = string.Format(_privateRepoBlobContainerNameFormat, subscriptionId.ToString());
            BlobContainerClient? blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            string? packageZipName = Path.GetFileName(packageLocation);
            BlobClient? blobClient = blobContainerClient.GetBlobClient(packageZipName);

            using FileStream? uploadFileStream = File.OpenRead(packageLocation);
            await blobClient.UploadAsync(uploadFileStream, true);
            uploadFileStream.Close();
        }

        public Uri GetDownloadUriForPackageUsingSas(string packageName, Guid? subscriptionId = null)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            BlobClient? blobClient = _publicRepositoryContainer.GetBlobClient(packageName);

            if (subscriptionId.HasValue)
            {
                blobClient = _blobServiceClient
                    .GetBlobContainerClient(string.Format(_privateRepoBlobContainerNameFormat, subscriptionId.ToString()))
                    .GetBlobClient(packageName);
            }

            BlobSasBuilder? sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(1))
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                Protocol = SasProtocol.Https,
            };

            BlobUriBuilder? uriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(_storageSharedKeyCredential)
            };

            return uriBuilder.ToUri();
        }

        public async Task<DownloadInfoDto> DownloadProfilePictureAsync(string userId)
        {
            try
            {
                BlobClient? client = _profilePictureContainer.GetBlobClient(userId);

                Azure.Response<BlobDownloadInfo>? result = await client.DownloadAsync();

                return new DownloadInfoDto
                {
                    ContentName = userId,
                    ContentType = result.Value.ContentType,
                    Content = Task.FromResult(result.Value.Content)
                };
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task UploadProfilePictureAsync(string userId, Stream picture)
        {
            picture.Position = 0;
            BlobClient? client = _profilePictureContainer.GetBlobClient(userId);

            await client.UploadAsync(picture, true);
        }

        public async Task RemoveProfilePictureAsync(string userId)
        {
            await _profilePictureContainer.DeleteBlobIfExistsAsync(userId);
        }

        public async Task<string> DownloadPrivateRepositoryPackageAsync(string packageName, Guid subscriptionId)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            if (subscriptionId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            string? blobContainerName = string.Format(_privateRepoBlobContainerNameFormat, subscriptionId.ToString());
            BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(blobContainerName);
            BlobClient blob = container.GetBlobClient(packageName);

            DirectoryInfo tempIntuneFolder = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Path.GetFileNameWithoutExtension(packageName));

            string fullPathDownloadedZip = Path.Combine(tempIntuneFolder.FullName, $"{Guid.NewGuid()}.zip");

            using FileStream? fileStream = File.OpenWrite(fullPathDownloadedZip);
            await blob.DownloadToAsync(fileStream);

            return fullPathDownloadedZip;
        }

        public async Task<string> DownloadPublicRepositoryPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            BlobClient blob = _publicRepositoryContainer.GetBlobClient(packageName);

            DirectoryInfo tempIntuneFolder = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Path.GetFileNameWithoutExtension(packageName));

            string fullPathDownloadedZip = Path.Combine(tempIntuneFolder.FullName, packageName);

            using FileStream? fileStream = File.OpenWrite(fullPathDownloadedZip);
            await blob.DownloadToAsync(fileStream);

            return fullPathDownloadedZip;
        }

        public async Task<DownloadInfoDto> DownloadLogoAsync(Guid subscriptionId)
        {
            try
            {
                BlobClient? client = _logoContainer.GetBlobClient(subscriptionId.ToString());

                Azure.Response<BlobDownloadInfo>? result = await client.DownloadAsync();

                return new DownloadInfoDto
                {
                    ContentName = subscriptionId.ToString(),
                    ContentType = result.Value.ContentType,
                    Content = Task.FromResult(result.Value.Content)
                };
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task UploadLogoAsync(Guid subscriptionId, Stream picture)
        {
            picture.Position = 0;
            BlobClient? client = _logoContainer.GetBlobClient(subscriptionId.ToString());

            await client.UploadAsync(picture, true);
        }

        public async Task RemoveLogoAsync(Guid subscriptionId)
        {
            await _logoContainer.DeleteBlobIfExistsAsync(subscriptionId.ToString());
        }
    }
}