using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Enums;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IShoppingService
    {
        /// <summary>
        /// Lists all the applications on the shopping app and do the pagination
        /// </summary>
        /// <param name="user">The user that is currently logged in</param>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="requestStateFilter">The state of the request by which we filter the applications that need to be shown</param>
        /// <returns></returns>
        Task<PagedResult<ShoppingApplicationDto>> ListApplicationsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            RequestState requestStateFilter);

        /// <summary>
        /// Lists all the requests on the Request page of shopping app and do the pagination
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="requestStateFilter">The state of the request by which we filter the applications that need to be shown</param>
        /// <param name="managerId">The id of the manager that gets the request from his subordinates</param>
        /// <returns></returns>
        Task<PagedResult<ShoppingRequestDto>> ListRequestsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            RequestState requestStateFilter,
            string? managerId);

        /// <summary>
        /// Gets the private application from the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationId">The id of the application we want to get</param>
        /// <returns></returns>
        Task<ShoppingApplicationDetailsDto> GetPrivateApplicationAsync(int applicationId);

        /// <summary>
        /// Gets the public application from the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationId">The id of the application we want to get</param>
        /// <returns></returns>
        Task<ShoppingApplicationDetailsDto> GetPublicApplicationAsync(int applicationId);

        /// <summary>
        /// Add a new private application from EndpointAdmin on the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopAddDto">The dto of the application that will be added</param>
        /// <returns></returns>
        Task AddPrivateApplicationsAsync(ShopAddDto dto);

        /// <summary>
        /// Add a new public application from EndpointAdmin on the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopAddDto">The dto of the application that will be added</param>
        /// <returns></returns>
        Task AddPublicApplicationsAsync(ShopAddDto dto);

        /// <summary>
        /// Delete a private application from the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopAddDto">The dto of the application that will be removed</param>
        /// <returns></returns>
        Task RemovePrivateApplicationsAsync(ShopRemoveDto dto);

        /// <summary>
        /// Remove a public application from the Shopping app
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopAddDto">The dto of the application that will be removed</param>
        /// <returns></returns>
        Task RemovePublicApplicationsAsync(ShopRemoveDto dto);

        /// <summary>
        /// Handles the request action of a public application from Shopping app
        /// </summary>
        /// <param name="requester">The author of the request action</param>
        /// <param name="applicationId">The id of the application we want to make a request for</param>
        /// <returns></returns>
        Task RequestPublicApplicationAsync(int applicationId);

        /// <summary>
        /// Handles the request action of a private application from Shopping app
        /// </summary>
        /// <param name="requester">The author of the request action</param>
        /// <param name="applicationId">The id of the application we want to make a request for</param>
        /// <returns></returns>
        Task RequestPrivateApplicationAsync(int applicationId);

        /// <summary>
        /// Approves a request for a private application by the Admin of the subscription
        /// </summary>
        /// <param name="user">The author of the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ApprovePrivateRequestAsync(long requestId);

        /// <summary>
        /// Approves a request for a public application by the Admin of the subscription
        /// </summary>
        /// <param name="user">The author of the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ApprovePublicRequestAsync(long requestId);

        /// <summary>
        /// Rejects a request for a private application by the Admin of the subscription
        /// </summary>
        /// <param name="user">The author of the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task RejectPrivateRequestAsync(long requestId);

        /// <summary>
        /// Rejects a request for a public application by the Admin of the subscription
        /// </summary>
        /// <param name="user">The author of the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task RejectPublicRequestAsync(long requestId);

        /// <summary>
        /// Approves a request for a private application by the Manager of the user that made the request
        /// </summary>
        /// <param name="simpleUser">The shop app user that made the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ManagerApprovePrivateRequestAsync(long requestId);

        /// <summary>
        /// Approves a request for a public application by the Manager of the user that made the request
        /// </summary>
        /// <param name="simpleUser">The shop app user that made the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ManagerApprovePublicRequestAsync(long requestId);

        /// <summary>
        /// Rejects a request for a private application by the Manager of the user that made the request
        /// </summary>
        /// <param name="simpleUser">The shop app user that made the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ManagerRejectPrivateRequestAsync(long requestId);

        /// <summary>
        /// Rejects a request for a public application by the Manager of the user that made the request
        /// </summary>
        /// <param name="simpleUser">The shop app user that made the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns></returns>
        Task ManagerRejectPublicRequestAsync(long requestId);

        /// <summary>
        /// Gets all the shop requests of the current subscription and counts them
        /// </summary>
        /// <returns></returns>
        Task<int> GetRequestsCountAsync();
    }
}