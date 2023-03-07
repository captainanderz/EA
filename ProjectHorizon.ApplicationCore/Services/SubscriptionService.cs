using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.DTOs.Billing;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IDeployIntunewinService _deployIntunewinService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IFarPayService _farPayService;
        private readonly IEconomicService _economicService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly Billing _billingOptions;
        private readonly IDeploymentScheduleJobService _deploymentScheduleJobService;

        private readonly int _minimumDaysInTrial = 30;
        private readonly IAzureBlobService _azureBlobService;

        public SubscriptionService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            IDeployIntunewinService deployIntunewinService,
            IBackgroundJobService backgroundJobService,
            IFarPayService farPayService,
            IEconomicService economicService,
            IAuditLogService auditLogService,
            ILoggedInUserProvider loggedInUserProvider,
            IOptions<Billing> billingOptions,
            IAzureBlobService azureBlobService,
            IDeploymentScheduleJobService deploymentScheduleJobService)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _deployIntunewinService = deployIntunewinService;
            _backgroundJobService = backgroundJobService;
            _farPayService = farPayService;
            _economicService = economicService;
            _auditLogService = auditLogService;
            _loggedInUserProvider = loggedInUserProvider;
            _billingOptions = billingOptions.Value;
            _azureBlobService = azureBlobService;
            _deploymentScheduleJobService = deploymentScheduleJobService;
        }

        /// <summary>
        /// Gets a subscription based on it's id
        /// </summary>
        /// <param name="id">The id of subscription we want to get</param>
        /// <returns>The subscription that has the given id</returns>
        public async Task<SubscriptionDto> GetSubscriptionAsync(Guid id)
        {
            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == id);

            SubscriptionDto? subscriptionDTO = _mapper.Map<SubscriptionDto>(subscription);

            return subscriptionDTO;
        }

        /// <summary>
        /// Lists all subscriptions and paginates them
        /// </summary>
        /// <param name="pageNumber">The number from which than pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific subscription by name</param>
        /// <returns>A paged result with all subscriptions paged</returns>
        public async Task<PagedResult<SubscriptionDto>> GetSubscriptionsPagedAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            IQueryable<Subscription>? querySubscriptions = _applicationDbContext.Subscriptions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                querySubscriptions = querySubscriptions.Where(s => s.Name.Contains(searchTerm.Trim()));
            }

            return new PagedResult<SubscriptionDto>
            {
                AllItemsCount = await querySubscriptions.CountAsync(),
                PageItems = await querySubscriptions
                   .OrderBy(subscription => subscription.Name)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .Select(subscription => new SubscriptionDto
                   {
                       Id = subscription.Id,
                       Name = subscription.Name,
                       State = subscription.State,
                       NumberOfUsers = subscription.SubscriptionUsers.Count,
                       DeviceCount = subscription.DeviceCount,
                       CreatedOn = subscription.CreatedOn,
                       LogoSmall = subscription.LogoSmall,
                   })
                   .ToListAsync()
            };
        }

        /// <summary>
        /// Filters from the list of subscriptions by name input
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription by which we want to filter</param>
        /// <returns>An enumerable with all subscription that has or contain the name we filter them by</returns>
        public async Task<IEnumerable<SubscriptionDto>> FilterSubscriptionsByNameAsync(string subscriptionName)
            => await _applicationDbContext
                .Subscriptions
                .Where(sub => sub.Name.ToLower().StartsWith(subscriptionName.ToLower()))
                .OrderBy(sub => sub.Name)
                .Take(5)
                .ProjectTo<SubscriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

        /// <summary>
        /// Set the current subscription to be updated with the auto-update option
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="autoUpdate">A bool determining if the auto-update option is enabled or not</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        public async Task<bool> UpdateSubscriptionAutoUpdateAsync(Guid subscriptionId, bool autoUpdate)
        {
            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == subscriptionId);

            subscription.GlobalAutoUpdate = autoUpdate;

            if (autoUpdate)
            {
                subscription.GlobalManualApprove = false;
            }

            await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(subPubApp => subPubApp.SubscriptionId == subscriptionId)
                .ForEachAsync(subPubApp => 
                { 
                    subPubApp.AutoUpdate = autoUpdate;
                    subPubApp.ManualApprove = subscription.GlobalManualApprove;
                });

            await _applicationDbContext.SaveChangesAsync();

            if (subscription.GlobalAutoUpdate)
            {
                await EnqueueDeployJobs(subscriptionId);
            }

            return autoUpdate;
        }

        /// <summary>
        /// Organize the deploy jobs and starts them in a specific order
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        private async Task EnqueueDeployJobs(Guid subscriptionId)
        {
            var subPubApps =  await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(
                    subPubApp => subPubApp.SubscriptionId == subscriptionId &&
                                 subPubApp.DeploymentStatus != DeploymentStatus.SuccessfulUpToDate &&
                                 subPubApp.DeploymentStatus != DeploymentStatus.InProgress &&
                                 subPubApp.AutoUpdate)
                .Include(subPubApp => subPubApp.DeploymentSchedule)
                .ToListAsync();

            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            foreach (SubscriptionPublicApplication? subPubApp in subPubApps)
            {
                if (!subPubApp.ManualApprove )
                {
                    if (subPubApp.DeploymentSchedule is null)
                    {
                        _backgroundJobService.Enqueue(() => _deployIntunewinService.DeployPublicApplicationForSubscriptionAsync(loggedInUser, subPubApp.SubscriptionId, subPubApp.PublicApplicationId, null, false));
                    }
                    else
                    {
                        var deploymentSchedule = subPubApp.DeploymentSchedule;
                        var inProgress = subPubApp
                            .DeploymentScheduleApplications
                            .Where(a => a.Type == DeploymentScheduleApplicationType.Current)
                            .Any();

                        if (deploymentSchedule.CronTrigger is null && !inProgress)
                        {
                            var application = (subPubApp.PublicApplicationId, false);
                            _backgroundJobService.Enqueue(
                                () => TriggerDeploymentSchedule(loggedInUser, subPubApp.SubscriptionId, deploymentSchedule.Id, application)
                                );
                        }
                    }
                }
            }
        }

        public async Task TriggerDeploymentSchedule(UserDto loggedInUser, Guid subscriptionId, long deploymentScheduleId, (int Id, bool IsPrivate) application)
        {
            await _deploymentScheduleJobService.TriggerJobAsync(loggedInUser, subscriptionId, deploymentScheduleId, application);
        }

        /// <summary>
        /// Set the current subscription to be updated with the manual-approve option
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="manualApprove">A bool determining if the auto-update option is enabled or not</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        public async Task<bool> UpdateSubscriptionManualApproveAsync(Guid subscriptionId, bool manualApprove)
        {
            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleAsync(sub => sub.Id == subscriptionId);

            subscription.GlobalManualApprove = manualApprove;

            if (manualApprove)
            {
                subscription.GlobalAutoUpdate = false;
            }

            var subPubApps = await _applicationDbContext
                .SubscriptionPublicApplications
                .Where(subPubApp => subPubApp.SubscriptionId == subscriptionId)
                .Include(subPubApp => subPubApp.DeploymentSchedule)
                .ToListAsync();

            foreach (var subPubApp in subPubApps)
            {
                if (subPubApp.DeploymentSchedule is not null)
                {
                    subPubApp.ManualApprove = false;
                }
                else
                {
                    subPubApp.ManualApprove = manualApprove;
                    subPubApp.AutoUpdate = subscription.GlobalAutoUpdate;
                }
            }

            await _applicationDbContext.SaveChangesAsync();

            return manualApprove;
        }

        /// <summary>
        /// Gets the payment order and card info used for a subscription
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription we want to get the payment and card information</param>
        /// <returns>A farpay response containing information about the payment order of a subscription</returns>
        public async Task<Response<FarPayResult>> GetOrderAndSetCardInfo(Guid subscriptionId)
        {
            Response<FarPayResult>? result = new Response<FarPayResult>()
            {
                IsSuccessful = false,
                ErrorMessage = $"Could not get order information"
            };

            Subscription? subscription = await _applicationDbContext.Subscriptions.SingleOrDefaultAsync(sub => sub.Id == subscriptionId);

            if (subscription == null)
            {
                return result;
            }

            Response<FarPayResult>? orderResult = await _farPayService.GetOrderAsync(subscription.FarPayToken);

            if (orderResult.IsSuccessful)
            {
                Response<FarPayCustomerResponse>? customer = await _farPayService.GetCustomerAsync(subscription.CustomerNumber);

                if (!customer.IsSuccessful)
                {
                    result.ErrorMessage = "Cannot get customer information";
                    return result;
                }

                string cardDetails = customer.Dto?.Agreement?.Details;
                if (!string.IsNullOrEmpty(cardDetails))
                {
                    string lastDigits = cardDetails.Substring(cardDetails.LastIndexOf('X') + 1, 4);
                    if (!string.IsNullOrEmpty(lastDigits) && int.TryParse(lastDigits, out _))
                    {
                        subscription.CreditCardDigits = lastDigits;

                        if (subscription.State == SubscriptionState.PaymentNotSetUp)
                        {
                            subscription.State = SubscriptionState.Trial;
                        }

                        await _applicationDbContext.SaveChangesAsync();
                    }
                }
            }

            return orderResult;
        }

        /// <summary>
        /// Updates the billing information of a subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to update the billing information</param>
        /// <param name="billingInfoDto">The billing information we want to update</param>
        /// <returns>A bool determining if the update was successful or not</returns>
        public async Task<bool> UpdateSubscriptionBillingAsync(Guid subscriptonId, BillingInfoDto billingInfoDto)
        {
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptonId);

            subscription.CompanyName = billingInfoDto.CompanyName;
            subscription.Country = billingInfoDto.Country;
            subscription.City = billingInfoDto.City;
            subscription.Email = billingInfoDto.SubscriptionEmail;
            subscription.ZipCode = billingInfoDto.ZipCode;
            subscription.VatNumber = billingInfoDto.VatNumber;

            await _applicationDbContext.SaveChangesAsync();

            EconomicCustomerDto? economicDto = _economicService.GetEconomicCustomerDto(billingInfoDto);

            if (subscription.CustomerNumber != null)
            {
                economicDto.CustomerNumber = subscription.CustomerNumber;

                return await _economicService.UpdateEconomicCustomerAsync(economicDto);
            }
            else
            {
                Response<EconomicCustomerDto>? result = await _economicService.CreateEconomicCustomerAsync(economicDto);

                if (result.IsSuccessful)
                {
                    subscription.CustomerNumber = result.Dto.CustomerNumber;

                    await _applicationDbContext.SaveChangesAsync();
                }

                return result.IsSuccessful;
            }
        }

        /// <summary>
        /// Gets the details of a subscription like expiration date and price
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to get details from</param>
        /// <returns>A subscription details dto representing the information about a given subscription</returns>
        public async Task<Response<SubscriptionDetailsDto>> GetSubscriptionDetailsAsync(Guid subscriptionId)
        {
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptionId);

            int billingDayOfMonth = _billingOptions.DayOfMonth;

            DateTime dueDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, billingDayOfMonth, 0, 0, 0, DateTimeKind.Utc);
            if (DateTime.UtcNow >= dueDate)
            {
                dueDate = dueDate.AddMonths(1);
            }

            if (subscription.State == SubscriptionState.Trial)
            {
                int daysInTrial = (dueDate - subscription.CreatedOn.Date).Days;
                if (daysInTrial < _minimumDaysInTrial)
                {
                    dueDate = dueDate.AddMonths(1);

                    daysInTrial = (dueDate - subscription.CreatedOn.Date).Days;
                    if (daysInTrial < _minimumDaysInTrial) // it's possible for CreatedOn, for example, 24 February
                    {
                        dueDate = dueDate.AddMonths(1);
                    }
                }
            }

            decimal dueAmount = _billingOptions.MonthlySubscriptionPrice;

            if (subscription.DeviceCount != null)
            {
                dueAmount += subscription.DeviceCount.Value * _billingOptions.PricePerEndpoint;
            }

            SubscriptionDetailsDto? subscriptionDetails = new SubscriptionDetailsDto()
            {
                SubscriptionId = subscription.Id,
                State = subscription.State,
                City = subscription.City,
                Country = subscription.Country,
                CompanyName = subscription.CompanyName,
                ZipCode = subscription.ZipCode,
                VatNumber = subscription.VatNumber,
                CreditCardDigits = subscription.CreditCardDigits,
                DueAmount = dueAmount,
                DueDate = dueDate,
                DaysUntilPayment = (dueDate - DateTime.UtcNow.Date).Days,
                SubscriptionEmail = subscription.Email,
                DeviceCount = subscription.DeviceCount,
                LogoSmall = subscription.LogoSmall,
                ShopGroupPrefix = subscription.ShopGroupPrefix,
            };

            return new Response<SubscriptionDetailsDto>()
            {
                IsSuccessful = true,
                Dto = subscriptionDetails
            };
        }

        /// <summary>
        /// Cancels a given subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the subscription we want to cancel</param>
        /// <returns>Void</returns>
        public async Task CancelSubscriptionAsync(Guid subscriptonId)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptonId);

            subscription.State = SubscriptionState.Cancelled;
            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.Subscription, 
                AuditLogActions.SubscriptionCancelled, 
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Reactivates a given subscription
        /// </summary>
        /// <param name="subscriptonId">The id of the current subscription</param>
        /// <returns>Void</returns>
        public async Task ReactivateSubscriptionAsync(Guid subscriptonId)
        {
            var loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptonId);

            if (WasSubscriptionInTrialBeforeCancellation(subscription))
            {
                subscription.State = SubscriptionState.Trial;
            }
            else
            {
                subscription.State = SubscriptionState.Active;
            }

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.Subscription,
                AuditLogActions.SubscriptionReactivated,
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task AddConsentAsync(Guid subscriptionId, SubscriptionConsentDto dto)
        {
            var consent = _mapper.Map<SubscriptionConsent>(dto);
            consent.SubscriptionId = subscriptionId;

            _applicationDbContext
                .SubscriptionConsents
                .Add(consent);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<SubscriptionConsentDto>> GetConsentsAsync(Guid subscriptionId)
        {
            var consents = await _applicationDbContext
                .SubscriptionConsents
                .Where(consent => consent.SubscriptionId == subscriptionId)
                .ProjectTo<SubscriptionConsentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return consents;
        }

        /// <summary>
        /// Checks if the subscription was in trial period before it was canceled
        /// </summary>
        /// <param name="subscription">The subscription we want to check if it was in trial period before it was canceled</param>
        /// <returns>A bool determining if the subscription was in trial period before being canceled</returns>
        private bool WasSubscriptionInTrialBeforeCancellation(Subscription subscription)
        {
            int billingDayOfMonth = _billingOptions.DayOfMonth;

            DateTime dueDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, billingDayOfMonth, 0, 0, 0, DateTimeKind.Utc);
            if (DateTime.UtcNow >= dueDate)
            {
                dueDate = dueDate.AddMonths(1);
            }

            DateTime dueDatePreviousMonth = dueDate.AddMonths(-1);

            return subscription.CreatedOn.Date >= dueDatePreviousMonth.AddDays(-_minimumDaysInTrial);
        }

        /// <summary>
        /// Changes the company logo 
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="picture">The logo picture we want to change</param>
        /// <returns>Void</returns>
        public async Task ChangeLogoAsync(Guid subscriptionId, MemoryStream picture)
        {
            using TransactionScope? scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptionId);
            subscription.LogoSmall = await ApplicationHelper.GetSmallLogoAsBase64Async(picture);
            await _applicationDbContext.SaveChangesAsync();

            await _azureBlobService.UploadLogoAsync(subscriptionId, picture);

            scope.Complete();
        }

        /// <summary>
        /// Deletes the company logo
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <returns>Void</returns>
        public async Task DeleteLogoAsync(Guid subscriptionId)
        {
            using TransactionScope? scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptionId);
            subscription.LogoSmall = string.Empty;
            await _applicationDbContext.SaveChangesAsync();

            await _azureBlobService.RemoveLogoAsync(subscriptionId);
            scope.Complete();
        }

        /// <summary>
        /// Updates the shop group prefix
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public async Task UpdateShopGroupPrefix(Guid subscriptionId, string prefix)
        {
            Subscription subscription = (await _applicationDbContext.Subscriptions.FindAsync(subscriptionId))!;
            subscription.ShopGroupPrefix = prefix;

            await _applicationDbContext.SaveChangesAsync();
        }
    }
}