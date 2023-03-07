using Microsoft.Graph;
using Microsoft.Identity.Client;
using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class GraphAuthProvider : IAuthenticationProvider
    {
        private readonly GraphConfigDto graphConfigDto;
        private readonly string instance = "https://login.microsoftonline.com/{0}";
        private readonly string apiUrl = "https://graph.microsoft.com/";

        internal string Authority => string.Format(CultureInfo.InvariantCulture, instance, graphConfigDto.Tenant);

        public GraphAuthProvider(GraphConfigDto graphConfigDto)
        {
            this.graphConfigDto = graphConfigDto ?? throw new ArgumentNullException(nameof(graphConfigDto));
        }

        public async Task<string> GetAccessToken()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(graphConfigDto.ClientId)
                .WithClientSecret(graphConfigDto.ClientSecret)
                .WithAuthority(new Uri(Authority))
                .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{apiUrl}.default" };

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            if (result != null)
            {
                return result.AccessToken;
            }

            return null;
        }

        // This is the required function to implement IAuthenticationProvider
        // The Graph SDK will call this function each time it makes a Graph
        // call.
        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", await GetAccessToken());
        }
    }
}
