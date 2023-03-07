using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.IntegrationTests
{
    public class CommonFixture : IAsyncLifetime, IDisposable
    {
        internal TestServer TestServer { get; }

        internal HttpClient Client { get; }

        internal UserDto CurrentTestUser { get; set; }

        internal Guid CurrentSubscriptionId { get; set; }

        internal int CurrentApplicationId { get; set; }

        private readonly WebHostBuilder webHostBuilder;

        public CommonFixture()
        {
            webHostBuilder = new WebHostBuilder();
            webHostBuilder.UseStartup<TestStartup>()
                .UseConfiguration(new ConfigurationBuilder()
                .AddJsonFile("testsettings.json")
                .Build());

            TestServer = new TestServer(webHostBuilder);
            Client = TestServer.CreateClient();
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task InitializeAsync()
        {
            //authorization
            CurrentTestUser = await GetTestUserAsync();
            CurrentSubscriptionId = Guid.Parse(webHostBuilder.GetSetting("TestSettings:TestSubscriptionId"));
        }

        internal GraphConfigDto GetTestGraphConfig()
        {
            GraphConfigDto graphConfigDto = new GraphConfigDto
            {
                ClientId = webHostBuilder.GetSetting("TestSettings:MicrosoftIntuneInformation:ClientId"),
                ClientSecret = webHostBuilder.GetSetting("TestSettings:MicrosoftIntuneInformation:ClientSecret"),
                Tenant = webHostBuilder.GetSetting("TestSettings:MicrosoftIntuneInformation:Tenant"),
                SubscriptionId = CurrentSubscriptionId
            };

            return graphConfigDto;
        }

        internal async Task<UserDto> GetTestUserAsync()
        {
            HttpRequestMessage? loginRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");

            Dictionary<string, string>? loginInput = new Dictionary<string, string>
            {
                {"email", TestConstants.TestUserEmail},
                {"password", TestConstants.TestPassword}
            };

            loginRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(loginInput), Encoding.Default, "application/json");

            HttpResponseMessage loginResponseMessage = await Client.SendAsync(loginRequestMessage);
            string loginContentResponse = await loginResponseMessage.Content.ReadAsStringAsync();
            Response<UserDto> userDtoResponse = JsonConvert.DeserializeObject<Response<UserDto>>(loginContentResponse);

            return userDtoResponse.Dto;
        }

        internal async Task<int> AddTestPublicApplicationAsync(PublicApplicationDto publicApplicationDto)
        {
            //prepare add app request message
            HttpRequestMessage? addAppRequestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/public-applications/add-or-update");
            addAppRequestMessage.Headers.Add("Authorization", $"bearer {CurrentTestUser.AccessToken}");
            string addAppJsonString = JsonConvert.SerializeObject(publicApplicationDto);

            addAppRequestMessage.Content = new StringContent(addAppJsonString, Encoding.Default, "application/json");

            HttpResponseMessage addAppResponseMessage = await Client.SendAsync(addAppRequestMessage);

            if (!addAppResponseMessage.IsSuccessStatusCode)
            {
                return -1;
            }

            string addAppResponseContent = await addAppResponseMessage.Content.ReadAsStringAsync();
            int createdAppId = int.Parse(addAppResponseContent);

            return createdAppId;
        }

        internal async Task<IEnumerable<PublicApplicationDto>> ListPublicApplicationsPagedAsync()
        {
            //fetch all public applications
            HttpResponseMessage listPubAppsResponse = await TestServer
                .CreateRequest($"/api/public-applications?pageNumber=1&pageSize=200")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("GET");

            string listAppsResult = await listPubAppsResponse.Content.ReadAsStringAsync();

            PagedResult<PublicApplicationDto>? pagedResult = JsonConvert.DeserializeObject<PagedResult<PublicApplicationDto>>(listAppsResult);

            return pagedResult.PageItems;
        }

        internal async Task<bool> RemovePublicApplicationAsync(int applicationId)
        {
            HttpResponseMessage? removeAppResponse = await TestServer.CreateRequest($"/api/public-applications/{applicationId}")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("DELETE");

            return removeAppResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> CreateTestGraphConfigAsync(GraphConfigDto graphConfigDto)
        {
            //prepare create graph config request message
            HttpRequestMessage? createGraphConfigMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/graph-config");
            createGraphConfigMessage.Headers.Add("Authorization", $"bearer {CurrentTestUser.AccessToken}");
            string createGraphConfigJsonString = JsonConvert.SerializeObject(graphConfigDto);

            createGraphConfigMessage.Content = new StringContent(createGraphConfigJsonString, Encoding.Default, "application/json");

            HttpResponseMessage addAppResponseMessage = await Client.SendAsync(createGraphConfigMessage);

            if (!addAppResponseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        internal async Task<bool> RemoveGraphConfigAsync()
        {
            HttpResponseMessage removeGraphConfigResponse = await TestServer.CreateRequest($"/api/graph-config")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("DELETE");

            return removeGraphConfigResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> CheckTestGraphConfigStatusAsync()
        {
            HttpResponseMessage checkGraphConfigResponse = await TestServer.CreateRequest($"/api/graph-config/status")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("GET");

            return checkGraphConfigResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> DeployApplication(int applicationId)
        {
            HttpRequestMessage? deployApplicationRequest = new HttpRequestMessage(HttpMethod.Post, "/api/public-applications/deploy");
            deployApplicationRequest.Headers.Add("Authorization", $"bearer {CurrentTestUser.AccessToken}");
            int[] appIds = { applicationId };

            deployApplicationRequest.Content = new StringContent(JsonConvert.SerializeObject(appIds), Encoding.Default, "application/json");
            HttpResponseMessage deployResponseMessage = await Client.SendAsync(deployApplicationRequest);

            return deployResponseMessage.IsSuccessStatusCode;
        }

        internal async Task<bool> RemoveDeployedApplication(int applicationId)
        {
            HttpResponseMessage removeDeployResponse = await TestServer.CreateRequest($"/api/public-applications/{applicationId}/remove-deploy")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("DELETE");

            return removeDeployResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> SetApplicationAutoUpdate(int applicationId, bool autoUpdate)
        {
            HttpResponseMessage setApplicationAutoUpdateResponse = await TestServer.CreateRequest($"/api/public-applications/{applicationId}/auto-update/{autoUpdate}")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("PATCH");

            return setApplicationAutoUpdateResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> SetApplicationManualApprove(int applicationId, bool manualApprove)
        {
            HttpResponseMessage setApplicationManualApproveResponse = await TestServer.CreateRequest($"/api/public-applications/{applicationId}/manual-approve/{manualApprove}")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("PATCH");

            return setApplicationManualApproveResponse.IsSuccessStatusCode;
        }

        internal async Task<bool> CheckDeployedApplication(int applicationId, string expectedVersion)
        {
            int maxRetryAttempts = 30;
            TimeSpan secondsBetweenFailures = TimeSpan.FromSeconds(10);

            Polly.Retry.AsyncRetryPolicy? retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(maxRetryAttempts, i => secondsBetweenFailures);

            HttpResponseMessage? checkDeployedApplicationResponse = new HttpResponseMessage();
            await retryPolicy.ExecuteAsync(async () =>
            {
                HttpResponseMessage checkDeployedApplicationResponse = await TestServer.CreateRequest($"/api/public-applications/{applicationId}/deployment")
                .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
                .SendAsync("GET");

                checkDeployedApplicationResponse.EnsureSuccessStatusCode();

                await CheckAppVersions(expectedVersion, checkDeployedApplicationResponse);
            });

            return checkDeployedApplicationResponse.IsSuccessStatusCode;
        }

        internal async Task<List<NotificationDto>> GetRecentNotifications()
        {
            HttpResponseMessage checkDeployedApplicationResponse = await TestServer.CreateRequest($"/api/notifications/recent")
            .AddHeader("Authorization", $"bearer {CurrentTestUser.AccessToken}")
            .SendAsync("GET");

            string recentNotificationsResult = await checkDeployedApplicationResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<NotificationDto>>(recentNotificationsResult);
        }

        internal async Task<bool> InviteUserAsync(UserInvitationDto userInvitationDto)
        {
            HttpRequestMessage? inviteUserMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/users/invite");
            inviteUserMessage.Headers.Add("Authorization", $"bearer {CurrentTestUser.AccessToken}");
            string userInvitationJsonString = JsonConvert.SerializeObject(userInvitationDto);

            inviteUserMessage.Content = new StringContent(userInvitationJsonString, Encoding.Default, "application/json");

            HttpResponseMessage inviteUserResponseMessage = await Client.SendAsync(inviteUserMessage);

            if (!inviteUserResponseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        internal async Task<bool> RegisterInvitationAsync(RegisterInvitationDto registerInvitationDto)
        {
            HttpRequestMessage? registerInvitationMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/register");
            registerInvitationMessage.Headers.Add("Authorization", $"bearer {CurrentTestUser.AccessToken}");
            string registerInvitationJsonString = JsonConvert.SerializeObject(registerInvitationDto);

            registerInvitationMessage.Content = new StringContent(registerInvitationJsonString, Encoding.Default, "application/json");

            HttpResponseMessage registerInvitationResponseMessage = await Client.SendAsync(registerInvitationMessage);

            if (!registerInvitationResponseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        private static async Task CheckAppVersions(string expectedVersion, HttpResponseMessage checkDeployedApplicationResponse)
        {
            string checkDeployedResult = await checkDeployedApplicationResponse.Content.ReadAsStringAsync();
            DeploymentInfoDto deploymentInfo = JsonConvert.DeserializeObject<DeploymentInfoDto>(checkDeployedResult);
            int versionCheck = ApplicationHelper.CompareAppVersions(expectedVersion, deploymentInfo.Version);

            //must be same version
            if (versionCheck != 0)
            {
                throw new HttpRequestException("Applications versions do not match");
            }
        }

        public async Task DisposeAsync()
        {
            await RemoveDeployedApplication(CurrentApplicationId);

            await RemovePublicApplicationAsync(CurrentApplicationId);

            await RemoveGraphConfigAsync();
        }

        public void Dispose()
        {

        }
    }
}
