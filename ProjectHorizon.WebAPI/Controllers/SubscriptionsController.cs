using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : HorizonBaseController
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IFarPayService _farPayService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IAzureOrganizationService _azureOrganizationService;

        public SubscriptionsController(ISubscriptionService subscriptionService, IFarPayService farPayService, IAzureBlobService azureBlobService, IAzureOrganizationService azureOrganizationService)
        {
            _subscriptionService = subscriptionService;
            _farPayService = farPayService;
            _azureBlobService = azureBlobService;
            _azureOrganizationService = azureOrganizationService;
        }

        [Authorize(Roles = UserRole.SuperAdmin)]
        [HttpGet("paged")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPagedSubscriptions([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? searchTerm)
        {
            return Ok(await _subscriptionService.GetSubscriptionsPagedAsync(pageNumber, pageSize, searchTerm));
        }

        [Authorize(Roles = UserRole.SuperAdmin)]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromQuery] string subscriptionName)
        {
            IEnumerable<SubscriptionDto> subscriptions = new List<SubscriptionDto>(0);

            if (!string.IsNullOrEmpty(subscriptionName))
            {
                subscriptions = await _subscriptionService.FilterSubscriptionsByNameAsync(subscriptionName);
            }

            return Ok(subscriptions);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPatch("[action]/{autoUpdate:bool}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> AutoUpdate(bool autoUpdate)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _subscriptionService
                .UpdateSubscriptionAutoUpdateAsync(loggedInUser.SubscriptionId, autoUpdate);

            return Ok(result);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator + "," + UserRole.Contributor)]
        [HttpPatch("[action]/{manualApprove:bool}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> ManualApprove(bool manualApprove)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _subscriptionService
                .UpdateSubscriptionManualApproveAsync(loggedInUser.SubscriptionId, manualApprove);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPatch("[action]/{subscriptionId:guid}")]
        [ProducesResponseType(typeof(Response<FarPayResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLastDigits(string subscriptionId)
        {
            if (Guid.TryParse(subscriptionId, out Guid subId))
            {
                Response<FarPayResult>? result = await _subscriptionService.GetOrderAndSetCardInfo(subId);

                return result.IsSuccessful ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError);
            }
            else
            {
                return BadRequest();
            }
        }

        [AllowAnonymous]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(Response<FarPayResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FarpayOrder([FromQuery] string subscriptionId)
        {
            if (Guid.TryParse(subscriptionId, out Guid subId))
            {
                SubscriptionDto? subscriptionDto = await _subscriptionService.GetSubscriptionAsync(subId);

                Response<FarPayResult>? result = await _farPayService.GetOrderAsync(subscriptionDto.FarPayToken);

                return result.IsSuccessful ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPost("[action]")]
        [ProducesResponseType(typeof(FarPayResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> NewFarpayOrder()
        {
            string? baseUrl = $"{Request.Scheme}://{Request.Host.Value}/";

            Response<FarPayResult>? result = await _farPayService.CreateOrderForExistingSubscriptionAsync();

            return result.IsSuccessful ? Ok(result.Dto) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPatch("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BillingInformation([FromBody] BillingInfoDto billingInfoDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            bool result = await _subscriptionService.UpdateSubscriptionBillingAsync(loggedInUser.SubscriptionId, billingInfoDto);

            return result ? Ok() : BadRequest();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpGet("[action]")]
        [ProducesResponseType(typeof(SubscriptionDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Details()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            Response<SubscriptionDetailsDto>? result = await _subscriptionService.GetSubscriptionDetailsAsync(loggedInUser.SubscriptionId);

            return result.IsSuccessful ? Ok(result.Dto) : BadRequest();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelSubscription()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            await _subscriptionService.CancelSubscriptionAsync(loggedInUser.SubscriptionId);

            return Ok();
        }

        [Authorize(Roles = UserRole.SuperAdmin + "," + UserRole.Administrator)]
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ReactivateSubscription()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            await _subscriptionService.ReactivateSubscriptionAsync(loggedInUser.SubscriptionId);

            return Ok();
        }

        [HttpGet("logo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLogo()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            DownloadInfoDto? response = await _azureBlobService.DownloadLogoAsync(loggedInUser.SubscriptionId);

            return response != null
                ? File(await response.Content, response.ContentType)
                : NotFound();
        }

        [HttpPut("[action]")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logo([FromForm] IFormFile file)
        {
            await using MemoryStream? memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            UserDto? loggedInUser = GetLoggedInUser();

            await _subscriptionService.ChangeLogoAsync(loggedInUser.SubscriptionId, memoryStream);

            return Ok();
        }

        [HttpDelete("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logo()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _subscriptionService.DeleteLogoAsync(loggedInUser.SubscriptionId);

            return Ok();
        }

        [HttpGet("organization")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrganization()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            var result = await _azureOrganizationService.GetAsync(loggedInUser.SubscriptionId);

            return Ok(result);
        }

        [HttpGet("consents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConsents()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            var consents = await _subscriptionService.GetConsentsAsync(loggedInUser.SubscriptionId);

            return Ok(consents);
        }

        [HttpPost("consents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddConsent([FromBody] SubscriptionConsentDto dto)
        {
            UserDto? loggedInUser = GetLoggedInUser();
            await _subscriptionService.AddConsentAsync(loggedInUser.SubscriptionId, dto);

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpGet("shopGroupPrefix")]
        public async Task<IActionResult> GetSubscriptionShopGroupPrefix()
        {
            UserDto? loggedInUser = GetLoggedInUser();
            var subscription = await _subscriptionService.GetSubscriptionDetailsAsync(loggedInUser.SubscriptionId);
            ShopGroupPrefixDto prefixDto = new ShopGroupPrefixDto();
            prefixDto.Prefix = subscription.Dto.ShopGroupPrefix;

            return Ok(prefixDto);
        }

        [HttpPut("shopGroupPrefix")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateShopGroupPrefix([FromBody] ShopGroupPrefixDto prefix)
        {
            UserDto? loggedInUser = GetLoggedInUser();
            await _subscriptionService.UpdateShopGroupPrefix(loggedInUser.SubscriptionId, prefix.Prefix);

            return Ok();
        }
    }
}
