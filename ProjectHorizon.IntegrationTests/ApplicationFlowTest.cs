using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.IntegrationTests
{
    public class ApplicationFlowTest : IClassFixture<CommonFixture>
    {
        private readonly string initialPackage = "TestApplication588.zip";
        private readonly string newPackage = "TestApplication592.zip";
        private readonly string initialPackageFilePath;
        private readonly string newPackageFilePath;
        private readonly Guid subscriptionId;

        internal CommonFixture _commonFixture;
        internal static DateTime startExecutionTime;

        public ApplicationFlowTest(CommonFixture commonFixture)
        {
            _commonFixture = commonFixture;

            //packages for add, deploy and update
            initialPackageFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"TestData\\{initialPackage}");
            newPackageFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"TestData\\{newPackage}");

            subscriptionId = Guid.NewGuid();
        }

        [Fact]
        public async Task DeployAndUpdatePublicApplicationSuccessfully()
        {
            startExecutionTime = DateTime.UtcNow;
            PublicApplicationDto initialPublicApplicationDto = await GetPublicApplicationDto(initialPackageFilePath);

            await AddAndListPublicApplication(initialPublicApplicationDto);

            await IntegrateWithIntune();

            await DeployPublicApplication(initialPublicApplicationDto);

            PublicApplicationDto newVersionPublicApplicationDto = await GetPublicApplicationDto(newPackageFilePath);

            //pass current app id to new dto, needed for setting auto update on current app
            newVersionPublicApplicationDto.Id = initialPublicApplicationDto.Id;

            await UpdateIntuneApplication(newVersionPublicApplicationDto);
        }

        #region Private methods
        private async Task AddAndListPublicApplication(PublicApplicationDto publicApplicationDto)
        {
            int applicationId = await _commonFixture.AddTestPublicApplicationAsync(publicApplicationDto);
            _commonFixture.CurrentApplicationId = applicationId;

            //check to see if add application response is successful          
            Assert.True(applicationId > 0, $"Invalid application id received");

            //get all public applications
            System.Collections.Generic.IEnumerable<PublicApplicationDto>? publicApplications = await _commonFixture.ListPublicApplicationsPagedAsync();

            //check if listing all public applications response is successful
            Assert.True(publicApplications.Any(), $"No public applications found");

            //check if app was actually added
            Assert.Contains(publicApplications, item => item.Name == publicApplicationDto.Name);
            publicApplicationDto.Id = applicationId;

            //check generated notification
            bool checkNotif = await CheckNotification(NotificationType.NewApplication);
            Assert.True(checkNotif, $"{NotificationType.NewApplication} not generated");
        }

        private async Task IntegrateWithIntune()
        {
            //integrate with Intune
            GraphConfigDto testGraphConfigDto = _commonFixture.GetTestGraphConfig();
            bool createTestGraphConfigResult = await _commonFixture.CreateTestGraphConfigAsync(testGraphConfigDto);
            Assert.True(createTestGraphConfigResult, $"Could not create graph configuration");

            //check graph configuration status
            bool checkTestGraphConfigStatusResult =
                await _commonFixture.CheckTestGraphConfigStatusAsync();
            Assert.True(checkTestGraphConfigStatusResult, $"Invalid status for graph configuration");
        }

        private async Task DeployPublicApplication(PublicApplicationDto publicApplicationDto)
        {
            //start deploy application
            bool startDeployApplicationResult = await _commonFixture
                .DeployApplication(publicApplicationDto.Id);
            Assert.True(startDeployApplicationResult, $"Could not start application deploy");

            //check if application has been deployed
            bool checkDeployApplicationResult = await _commonFixture
                .CheckDeployedApplication(publicApplicationDto.Id, publicApplicationDto.Version);
            Assert.True(checkDeployApplicationResult, $"Could not deploy application");
        }

        private async Task UpdateIntuneApplication(PublicApplicationDto publicApplicationDto)
        {
            //set auto update for application to true
            bool setAppAutoUpdateResult = await _commonFixture.SetApplicationAutoUpdate(publicApplicationDto.Id, true);
            Assert.True(setAppAutoUpdateResult, $"Could set app auto update");

            //set manual approve for application to false
            bool setAppManualApproveResult = await _commonFixture.SetApplicationManualApprove(publicApplicationDto.Id, false);
            Assert.True(setAppManualApproveResult, $"Could set app manual approve");

            //add new application version
            int updateApplicationId = await _commonFixture.AddTestPublicApplicationAsync(publicApplicationDto);
            //application Ids should be the same after update
            Assert.Equal(_commonFixture.CurrentApplicationId, updateApplicationId);

            //check if application has been deployed and updated
            bool checkDeployUpdatedApplicationResult = await _commonFixture
                .CheckDeployedApplication(publicApplicationDto.Id, publicApplicationDto.Version);
            Assert.True(checkDeployUpdatedApplicationResult, $"Could not deploy updated application");

            //check generated notification
            bool checkNotif = await CheckNotification(NotificationType.NewVersion);
            Assert.True(checkNotif, $"{NotificationType.NewVersion} not generated");
        }

        private async Task<PublicApplicationDto> GetPublicApplicationDto(string archiveFullPath)
        {
            Response<ApplicationDto> applicationResponse = await ApplicationHelper.ValidateAndGetInfo(archiveFullPath, true);

            string tempArchiveFileName = ApplicationHelper.GetTempArchiveFileNameFromInfo(applicationResponse.Dto);
            File.Move(archiveFullPath, Path.Combine(ApplicationHelper.GetMountPath(), tempArchiveFileName), true);

            //TODO: Temp solution here, should inject AutoMapper and use mapping instead
            string? applicationResponseJson = JsonConvert.SerializeObject(applicationResponse.Dto);
            PublicApplicationDto publicApplicationDto = JsonConvert.DeserializeObject<PublicApplicationDto>(applicationResponseJson);
            return publicApplicationDto;
        }

        private async Task<bool> CheckNotification(string notificationType)
        {
            System.Collections.Generic.List<NotificationDto>? recentNotifications = await _commonFixture.GetRecentNotifications();

            return recentNotifications.Any(item => !item.IsRead && item.Type == notificationType && item.CreatedOn.AddMinutes(1) > startExecutionTime);
        }
        #endregion Private methods
    }
}
