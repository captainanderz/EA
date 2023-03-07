using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface ISubscriptionService
    {
        /// <summary>
        /// Gets a subscription based on it's id
        /// </summary>
        /// <param name="id">The id of subscription we want to get</param>
        /// <returns>The subscription that has the given id</returns>
        Task<SubscriptionDto> GetSubscriptionAsync(Guid id);

        /// <summary>
        /// Set the current subscription to be updated with the auto-update option
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="autoUpdate">A bool determining if the auto-update option is enabled or not</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        Task<bool> UpdateSubscriptionAutoUpdateAsync(Guid subscriptionId, bool autoUpdate);

        /// <summary>
        /// Set the current subscription to be updated with the manual-approve option
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="manualApprove">A bool determining if the auto-update option is enabled or not</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        Task<bool> UpdateSubscriptionManualApproveAsync(Guid subscriptionId, bool manualApprove);

        /// <summary>
        /// Lists all subscriptions and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which than pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific subscription by name</param>
        /// <returns>A paged result with all subscriptions paged</returns>
        Task<PagedResult<SubscriptionDto>> GetSubscriptionsPagedAsync(int pageNumber, int pageSize, string? searchTerm);

        /// <summary>
        /// Updates the billing information of a subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to update the billing information</param>
        /// <param name="billingInfoDto">The billing information we want to update</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        Task<bool> UpdateSubscriptionBillingAsync(Guid subscriptonId, BillingInfoDto billingInfoDto);

        /// <summary>
        /// Gets the details of a subscription like expiration date and price
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to get details from</param>
        /// <returns>A subscription details dto representing the information about a given subscription</returns>
        Task<Response<SubscriptionDetailsDto>> GetSubscriptionDetailsAsync(Guid subscriptonId);

        /// <summary>
        /// Filters from the list of subscriptions by name input
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription by which we want to filter</param>
        /// <returns>An enumerable with all subscription that has or contain the name we filter them by</returns>
        Task<IEnumerable<SubscriptionDto>> FilterSubscriptionsByNameAsync(string subscriptionName);

        /// <summary>
        /// Gets the payment order and card info used for a subscription
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription we want to get the payment and card information</param>
        /// <returns>A farpay response containing information about the payment order of a subscription</returns>
        Task<Response<FarPayResult>> GetOrderAndSetCardInfo(Guid subscriptionId);

        /// <summary>
        /// Cancels a given subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to cancel</param>
        /// <returns>Void</returns>
        Task CancelSubscriptionAsync(Guid subscriptonId);

        /// <summary>
        /// Reactivates a given subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the current subscription</param>
        /// <returns>Void</returns>
        Task ReactivateSubscriptionAsync(Guid subscriptonId);

        /// <summary>
        /// Changes the company logo 
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="picture">The logo picture we want to change</param>
        /// <returns>Void</returns>
        Task ChangeLogoAsync(Guid subscriptionId, MemoryStream picture);


        /// <summary>
        /// Deletes the company logo
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns>Void</returns>
        Task DeleteLogoAsync(Guid subscriptionId);

        Task AddConsentAsync(Guid subscriptionId, SubscriptionConsentDto dto);

        Task<IEnumerable<SubscriptionConsentDto>> GetConsentsAsync(Guid subscriptionId);

        /// <summary>
        /// Updates the prefix of a shop group
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription</param>
        /// <param name="prefix">The prefix we want to add</param>
        /// <returns></returns>
        Task UpdateShopGroupPrefix(Guid subscriptionId, string prefix);
    }
}
