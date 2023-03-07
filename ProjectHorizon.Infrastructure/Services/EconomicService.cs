using Newtonsoft.Json;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class EconomicService : IEconomicService
    {
        private readonly HttpClient _httpClient;

        public EconomicService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Response<EconomicCustomerDto>> CreateEconomicCustomerAsync(EconomicCustomerDto economicCustomerDto)
        {
            Response<EconomicCustomerDto>? result = new Response<EconomicCustomerDto>()
            {
                IsSuccessful = false,
                ErrorMessage = "Could not create Economic customer"
            };

            StringContent? requestContent = new StringContent(JsonConvert.SerializeObject(economicCustomerDto),
                Encoding.Default, "application/json");

            HttpResponseMessage? postResult = await _httpClient.PostAsync("customers", requestContent);

            if (postResult.IsSuccessStatusCode)
            {
                string postResultJson = await postResult.Content.ReadAsStringAsync();

                try
                {
                    dynamic x = JsonConvert.DeserializeObject(postResultJson);
                    economicCustomerDto.CustomerNumber = x.customerNumber;
                }
                catch
                {
                    result.ErrorMessage = "Cannot read economic customer number";
                    return result;
                }

                result.Dto = economicCustomerDto;
                result.IsSuccessful = true;
                result.ErrorMessage = null;
            }

            return result;
        }

        public async Task<bool> UpdateEconomicCustomerAsync(EconomicCustomerDto economicCustomerDto)
        {
            StringContent? requestContent = new StringContent(JsonConvert.SerializeObject(economicCustomerDto),
                Encoding.Default, "application/json");

            HttpResponseMessage? result = await _httpClient.PutAsync($"customers/{economicCustomerDto.CustomerNumber}", requestContent);

            return result.IsSuccessStatusCode;
        }

        public EconomicCustomerDto GetEconomicCustomerDto(BillingInfoDto billingInfoDto)
        {
            return new EconomicCustomerDto
            {
                Currency = "EUR",
                Name = billingInfoDto.CompanyName,
                Country = billingInfoDto.Country,
                City = billingInfoDto.City,
                Email = billingInfoDto.SubscriptionEmail,
                Zip = billingInfoDto.ZipCode,
                VatNumber = billingInfoDto.VatNumber,
                PaymentTerms = new PaymentTerms()
                {
                    PaymentTermsNumber = 2
                },
                CustomerGroup = new CustomerGroup()
                {
                    CustomerGroupNumber = 1
                },
                VatZone = new VatZone()
                {
                    VatZoneNumber = 1
                }
            };
        }
    }
}