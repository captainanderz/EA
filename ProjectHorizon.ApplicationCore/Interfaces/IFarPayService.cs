using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using System;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IFarPayService
    {
        Task<Response<FarPayCustomerDto>> CreateCustomerAsync(FarPayCustomerDto farPayCustomerDto);

        Task<Response<FarPayResult>> CreateOrderAsync(FarPayOrderDto farPayOrderDto, string redirectToPage = null);

        Task<Response<FarPayResult>> CreateOrderForExistingSubscriptionAsync();

        Task<Response<FarPayResult>> GetOrderAsync(string farPayToken);

        Task<Response<FarPayCustomerResponse>> GetCustomerAsync(string customerNumber);

        FarPayCustomerDto GetFarPayCustomerDto(string customerNumber, BillingInfoDto billingInfo);

        FarPayOrderDto GetFarPayOrderDto(string customerNumber, Guid subscriptionId);
    }
}
