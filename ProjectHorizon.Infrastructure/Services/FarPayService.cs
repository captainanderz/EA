using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using System.Net.Http;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ProjectHorizon.Infrastructure.Services
{
    public class FarPayService : IFarPayService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ApplicationCore.Options.Environment _envOptions;

        //FarPay will redirect to this address after payment has been processed
        private const string paymentSetupPage = "payment-setup";
        private const string subscriptionPage = "subscription";

        public FarPayService(
            HttpClient httpClient,
            ILoggedInUserProvider loggedInUserProvider,
            IApplicationDbContext applicationDbContext,
            IOptions<ApplicationCore.Options.Environment> envOptions)
        {
            _httpClient = httpClient;
            _loggedInUserProvider = loggedInUserProvider;
            _applicationDbContext = applicationDbContext;
            _envOptions = envOptions.Value;
        }

        public async Task<Response<FarPayCustomerDto>> CreateCustomerAsync(FarPayCustomerDto farPayCustomerDto)
        {
            Response<FarPayCustomerDto>? result = new Response<FarPayCustomerDto>()
            {
                IsSuccessful = false,
                ErrorMessage = "Could not create FarPay customer"
            };

            StringContent? requestContent = new StringContent(JsonConvert.SerializeObject(farPayCustomerDto),
                Encoding.Default, "application/json");

            HttpResponseMessage? postResult = await _httpClient.PostAsync("customers", requestContent);

            if (postResult.IsSuccessStatusCode)
            {
                string postResultJson = await postResult.Content.ReadAsStringAsync();

                result.Dto = JsonConvert.DeserializeObject<FarPayCustomerDto>(postResultJson);
                result.IsSuccessful = true;
                result.ErrorMessage = null;
            }

            return result;
        }

        public async Task<Response<FarPayCustomerResponse>> GetCustomerAsync(string customerNumber)
        {
            Response<FarPayCustomerResponse>? result = new Response<FarPayCustomerResponse>()
            {
                IsSuccessful = false,
                ErrorMessage = $"Could not get customer information."
            };

            HttpResponseMessage? getCustomerResult = await _httpClient.GetAsync($"customers/{customerNumber}");

            if (getCustomerResult.IsSuccessStatusCode)
            {
                string getResultJson = await getCustomerResult.Content.ReadAsStringAsync();

                result.Dto = JsonConvert.DeserializeObject<FarPayCustomerResponse>(getResultJson);
                result.IsSuccessful = true;
                result.ErrorMessage = null;
            }
            else if (getCustomerResult.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.ErrorMessage = $"Customer not found";
            }

            return result;
        }

        public async Task<Response<FarPayResult>> GetOrderAsync(string farPayToken)
        {
            Response<FarPayResult>? result = new Response<FarPayResult>()
            {
                IsSuccessful = false,
                ErrorMessage = $"Could not get order information."
            };

            HttpResponseMessage? getResult = await _httpClient.GetAsync($"orders/{farPayToken}");

            if (getResult.IsSuccessStatusCode)
            {
                string getResultJson = await getResult.Content.ReadAsStringAsync();

                result.Dto = JsonConvert.DeserializeObject<FarPayResult>(getResultJson);
                result.IsSuccessful = true;
                result.ErrorMessage = null;
            }
            else if (getResult.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.ErrorMessage = $"Order not found";
            }

            return result;
        }

        public async Task<Response<FarPayResult>> CreateOrderAsync(FarPayOrderDto farPayOrderDto,
            string redirectToPage = null)
        {
            string baseUrl = _envOptions.BaseUrl;

            if (redirectToPage == null)
            {
                redirectToPage = paymentSetupPage;
            }

            string? redirectUrl = $"{baseUrl}{redirectToPage}?subscriptionId={farPayOrderDto.ExternalID}&paymentResult=";

            farPayOrderDto.AcceptUrl = $"{redirectUrl}accept";
            farPayOrderDto.CancelUrl = $"{redirectUrl}cancel";
            farPayOrderDto.CallbackUrl = $"{redirectUrl}callback";

            Response<FarPayResult>? result = new Response<FarPayResult>()
            {
                IsSuccessful = false,
                ErrorMessage = $"Could not create FarPay order for {farPayOrderDto.ExternalID}"
            };

            StringContent? requestContent = new StringContent(JsonConvert.SerializeObject(farPayOrderDto),
                Encoding.Default, "application/json");

            HttpResponseMessage? postOrderResult = await _httpClient.PostAsync("orders", requestContent);

            if (postOrderResult.IsSuccessStatusCode)
            {
                string postOrderResultJson = await postOrderResult.Content.ReadAsStringAsync();

                result.Dto = JsonConvert.DeserializeObject<FarPayResult>(postOrderResultJson);
                result.IsSuccessful = true;
                result.ErrorMessage = null;
            }

            return result;
        }

        public async Task<Response<FarPayResult>> CreateOrderForExistingSubscriptionAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            ApplicationCore.Entities.Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(loggedInUser.SubscriptionId);

            if (subscription.FarPayToken != null)
            {
                await RemoveOrderAsync(subscription.FarPayToken);
            }

            FarPayOrderDto? farPayOrderDto = GetFarPayOrderDto(subscription.CustomerNumber, subscription.Id);

            Response<FarPayResult>? createOrderResult = await CreateOrderAsync(farPayOrderDto, redirectToPage: subscriptionPage);

            if (createOrderResult.IsSuccessful)
            {
                subscription.FarPayToken = createOrderResult.Dto.Token;
                await _applicationDbContext.SaveChangesAsync();
            }

            return createOrderResult;
        }

        public async Task<bool> RemoveOrderAsync(string farPayToken)
        {
            HttpResponseMessage? deleteResult = await _httpClient.DeleteAsync($"orders?token={farPayToken}");

            return deleteResult.IsSuccessStatusCode;
        }

        public FarPayCustomerDto GetFarPayCustomerDto(string customerNumber, BillingInfoDto billingInfo)
        {
            return new FarPayCustomerDto
            {
                CustomerNumber = customerNumber,
                Name = billingInfo.CompanyName,
                City = billingInfo.City,
                Country = billingInfo.Country,
                PostCode = billingInfo.ZipCode,
                Gln = billingInfo.VatNumber,
                Email = billingInfo.SubscriptionEmail
            };
        }

        public FarPayOrderDto GetFarPayOrderDto(string customerNumber, Guid subscriptionId)
        {
            return new FarPayOrderDto
            {
                ExternalID = subscriptionId.ToString(),
                Lang = "en",
                PaymentTypes = "card",
                Agreement = 1,
                Customer = new Customer
                {
                    CustomerNumber = customerNumber
                }
            };
        }
    }
}