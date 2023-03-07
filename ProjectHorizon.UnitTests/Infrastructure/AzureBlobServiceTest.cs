using Microsoft.Extensions.Options;
using Moq;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.Infrastructure.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.Infrastructure
{
    public class AzureBlobServiceTest
    {
        private static readonly Mock<IOptions<AzureStorageInformation>> _mockOptions = new();

        public AzureBlobServiceTest()
        {
            AzureStorageInformation? azureStorageInformation = new AzureStorageInformation
            {
                AzureStorageConnection = "UseDevelopmentStorage=true",
                PublicRepositoryName = "public-container-mock",
                PackageCacheName = "intune-cache-mock",
                ProfilePicturesName = "profile-pictures-mock",
                LogosName = "logos-mock"
            };

            _mockOptions
                .SetupGet(m => m.Value)
                .Returns(azureStorageInformation);
        }

        [Fact]
        public async Task UploadPackageAsync_NullArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.UploadPublicRepositoryPackageAsync(null));
        }

        [Fact]
        public async Task UploadPackageAsync_EmptyStringArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.UploadPublicRepositoryPackageAsync(string.Empty));
        }

        [Fact]
        public async Task DownloadPackageAsync_NullArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.DownloadPublicRepositoryPackageAsync(null));
        }

        [Fact]
        public async Task DownloadPackageAsync_EmptyStringArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.DownloadPublicRepositoryPackageAsync(string.Empty));
        }

        [Fact]
        public async Task RemovePackageAsync_NullArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.RemovePublicRepositoryPackageAsync(null));
        }

        [Fact]
        public async Task RemovePackageAsync_EmptyStringArgumentValidationAsync()
        {
            //arrange
            IAzureBlobService packageService = new AzureBlobService(_mockOptions.Object);

            //act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => packageService.RemovePublicRepositoryPackageAsync(string.Empty));
        }
    }
}
