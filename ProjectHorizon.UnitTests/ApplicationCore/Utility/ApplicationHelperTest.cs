using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Utility
{
    public class ApplicationHelperTest
    {
        private readonly string validTestZipName = "valid_test_package.zip";
        private readonly string invalidTestZipName = "invalid_test_package.zip";
        private readonly string validTestZipFilePath;
        private readonly string invalidTestZipFilePath;
        private readonly PublicApplicationDto testPublicApplicationDto;
        private readonly Guid testSubscriptionId;

        public ApplicationHelperTest()
        {
            string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            validTestZipFilePath = Path.Combine(path, $"TestData\\{validTestZipName}");
            invalidTestZipFilePath = Path.Combine(path, $"TestData\\{invalidTestZipName}");

            testSubscriptionId = new Guid();

            testPublicApplicationDto = new PublicApplicationDto
            {
                Publisher = "Google",
                Name = "Google Chrome",
                RunAs32Bit = false,
                Version = "87.0.4280.88",
                Language = "en-US",
                Architecture = "x64"
            };
        }

        [Fact]
        public void CompareAppVersions_IncomingVersion_HigherThanCurrent()
        {
            // arrange
            string incomingVersion = "1.24.55.1487";
            string currentVersion = "1.24.55.487";

            // act
            int compareResult = ApplicationHelper.CompareAppVersions(incomingVersion, currentVersion);

            //assert
            Assert.True(compareResult > 0, "Incoming version should be higher");
        }

        [Fact]
        public async void ValidateAndGetInfo_ValidInput_ReturnsCorrectData()
        {
            // act
            Response<ApplicationDto>? publicApplicationResponse = await ApplicationHelper.ValidateAndGetInfo(validTestZipFilePath);

            // assert
            Assert.NotNull(publicApplicationResponse.Dto);
            Assert.Equal("Google Chrome", publicApplicationResponse.Dto.Name);
        }

        [Fact]
        public async Task ValidateAndGetInfo_InvalidInput_ThrowsException()
        {
            // act
            Response<ApplicationDto>? publicApplicationResponse = await ApplicationHelper.ValidateAndGetInfo(invalidTestZipFilePath);

            // assert
            Assert.False(publicApplicationResponse.IsSuccessful);
        }

        [Fact]
        public void GetTempArchiveFileNameFromInfo_ReturnsCorrectName()
        {
            // act
            string? tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(testPublicApplicationDto, testSubscriptionId);

            // assert
            Assert.Equal($"temp_Google_GoogleChrome_x64_87.0.4280.88_en-US_{testSubscriptionId}.zip", tempArchiveFileName);
        }

        [Fact]
        public void GetArchiveFileNameFromInfo_ReturnsCorrectName()
        {
            // act
            string? archiveFileName = ApplicationHelper.GetArchiveFileNameFromInfo(testPublicApplicationDto, testSubscriptionId);

            // assert
            Assert.Equal($"Google_GoogleChrome_x64_87.0.4280.88_en-US_{testSubscriptionId}.zip", archiveFileName);
        }

        [Fact]
        public void GetPackageNameFromInfo_ReturnsCorrectName()
        {
            // act
            string? packageName = ApplicationHelper.GetPackageNameFromInfo(testPublicApplicationDto, testSubscriptionId);

            // assert
            Assert.Equal($"Google_GoogleChrome_x64_87.0.4280.88_en-US_{testSubscriptionId}", packageName);
        }
    }
}
