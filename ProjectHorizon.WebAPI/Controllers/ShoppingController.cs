using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingController : HorizonBaseController
    {
        private readonly IShoppingService _shoppingService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILoggedInSimpleUserProvider _loggedInSimpleUserProvider;

        public ShoppingController(IShoppingService shoppingService, IAzureBlobService azureBlobService, ISubscriptionService subscriptionShoppingService, ILoggedInSimpleUserProvider loggedInSimpleUserProvider)
        {
            _shoppingService = shoppingService;
            _azureBlobService = azureBlobService;
            _subscriptionService = subscriptionShoppingService;
            _loggedInSimpleUserProvider = loggedInSimpleUserProvider;
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpGet("applications")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(PagedResult<ShoppingApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListApplications(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] RequestState requestStateFilter)
        {
            PagedResult<ShoppingApplicationDto>? result = await _shoppingService
                .ListApplicationsPagedAsync(pageNumber, pageSize, searchTerm, requestStateFilter);

            return Ok(result);
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpGet("subordinates/requests")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListSubordinatesRequests(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] RequestState requestStateFilter)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            PagedResult<ShoppingRequestDto>? result = await _shoppingService
                .ListRequestsPagedAsync(pageNumber, pageSize, searchTerm, requestStateFilter, loggedInUser.Id);

            return Ok(result);
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [EnableCors(CorsConstants.ShopPolicy)]
        [HttpDelete("subordinates/requests/private/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManagerRejectPrivateRequest([FromRoute] long requestId)
        {
            await _shoppingService.ManagerRejectPrivateRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [EnableCors(CorsConstants.ShopPolicy)]
        [HttpDelete("subordinates/requests/public/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManagerRejectPublicRequest([FromRoute] long requestId)
        {
            await _shoppingService.ManagerRejectPublicRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpPut("subordinates/requests/private/{requestId:long}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManagerApprovePrivateRequest([FromRoute] long requestId)
        {
            await _shoppingService.ManagerApprovePrivateRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpPut("subordinates/requests/public/{requestId:long}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManagerApprovePublicRequest([FromRoute] long requestId)
        {
            await _shoppingService.ManagerApprovePublicRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpGet("applications/private/{applicationId:int}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        public async Task<IActionResult> GetPrivateApplicationById([FromRoute] int applicationId)
        {
            ShoppingApplicationDetailsDto? applicationDetails = await _shoppingService.GetPrivateApplicationAsync(applicationId);

            return Ok(applicationDetails);
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpGet("applications/public/{applicationId:int}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        public async Task<IActionResult> GetPublicApplicationById([FromRoute] int applicationId)
        {
            ShoppingApplicationDetailsDto? applicationDetails = await _shoppingService.GetPublicApplicationAsync(applicationId);

            return Ok(applicationDetails);
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpPost("applications/public/{applicationId:int}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        public async Task<IActionResult> RequestPublicApplication([FromRoute] int applicationId)
        {
            await _shoppingService.RequestPublicApplicationAsync(applicationId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpPost("applications/private/{applicationId:int}")]
        [EnableCors(CorsConstants.ShopPolicy)]
        public async Task<IActionResult> RequestPrivateApplication([FromRoute] int applicationId)
        {
            await _shoppingService.RequestPrivateApplicationAsync(applicationId);

            return Ok();
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [EnableCors(CorsConstants.ShopPolicy)]
        [HttpGet("subscription-logo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLogo()
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            Response<SubscriptionDetailsDto>? result = await _subscriptionService.GetSubscriptionDetailsAsync(loggedInUser.SubscriptionId);

            if (!result.IsSuccessful)
            {
                return BadRequest();
            }

            SimpleSubscriptionDto? dto = new SimpleSubscriptionDto
            {
                Id = result.Dto.SubscriptionId,
                LogoSmall = result.Dto.LogoSmall
            };

            return Ok(dto);
        }

        [Authorize(Policy = AuthConstants.SimplePolicy)]
        [HttpGet("[action]")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(SubscriptionDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Details()
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            Response<SubscriptionDetailsDto>? result = await _subscriptionService.GetSubscriptionDetailsAsync(loggedInUser.SubscriptionId);

            return result.IsSuccessful ? Ok(result.Dto) : BadRequest();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("applications/private")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddPrivate([FromBody] ShopAddDto dto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _shoppingService
                .AddPrivateApplicationsAsync(dto);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPost("applications/public")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddPublic([FromBody] ShopAddDto dto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _shoppingService
                .AddPublicApplicationsAsync(dto);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("applications/private")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeletePrivate([FromBody] ShopRemoveDto dto)
        {
            await _shoppingService
                .RemovePrivateApplicationsAsync(dto);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("applications/public")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeletePublic([FromBody] ShopRemoveDto dto)
        {
            await _shoppingService
                .RemovePublicApplicationsAsync(dto);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("requests")]
        [EnableCors(CorsConstants.ShopPolicy)]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListRequests(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] RequestState requestStateFilter)
        {
            PagedResult<ShoppingRequestDto>? result = await _shoppingService
                .ListRequestsPagedAsync(pageNumber, pageSize, searchTerm, requestStateFilter, null);

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPut("requests/private/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApprovePrivateRequest([FromRoute] long requestId)
        {
            await _shoppingService.ApprovePrivateRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPut("requests/public/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApprovePublicRequest([FromRoute] long requestId)
        {
            await _shoppingService.ApprovePublicRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("requests/private/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectPrivateRequest([FromRoute] long requestId)
        {
            await _shoppingService.RejectPrivateRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpDelete("requests/public/{requestId:long}")]
        [ProducesResponseType(typeof(PagedResult<ShoppingRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectPublicRequest([FromRoute] long requestId)
        {
            await _shoppingService.RejectPublicRequestAsync(requestId);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor + "," + UserRole.Reader)]
        [HttpGet("requests/count")]
        public async Task<IActionResult> GetRequestCount()
        {
            int shopRequestCount = await _shoppingService.GetRequestsCountAsync();

            return Ok(new { Count = shopRequestCount});
        }
    }
}
