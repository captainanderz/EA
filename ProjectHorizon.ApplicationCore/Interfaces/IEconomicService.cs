using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IEconomicService
    {
        Task<Response<EconomicCustomerDto>> CreateEconomicCustomerAsync(EconomicCustomerDto economicCustomerDto);

        Task<bool> UpdateEconomicCustomerAsync(EconomicCustomerDto economicCustomerDto);

        EconomicCustomerDto GetEconomicCustomerDto(BillingInfoDto billingInfoDto);
    }
}
