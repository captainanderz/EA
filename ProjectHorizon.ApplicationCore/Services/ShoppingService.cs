using AutoMapper;
using AutoMapper.QueryableExtensions;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Enums;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Properties;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class ShoppingService : IShoppingService
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private const string ShopGroupNameFormat = "{0}_{1}_{2}_{3}";
        private const string ShopAssignmentProfileNameFormat = "Shop - {0}";
        private const string ShopRequestEmailSubject = "New Shop Request";
        private const string ShopResponseEmailSubject = "Shop request for {0} was {1}";
        private const string ShopRequestStateAccepted = "accepted";
        private const string ShopRequestStateRejected = "rejected";
        private const string ShopRequestResolverTypeAdmin = "an admin";
        private const string ShopRequestResolverTypeManager = "your manager";

        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly IAzureGroupService _azureGroupService;
        private readonly IAzureUserService _azureUserService;
        private readonly IAssignmentProfileService _assignmentProfileService;
        private readonly IEmailService _emailService;
        private readonly Options.Environment _envOptions;
        private readonly ILoggedInUserProvider _loggedInUserProvider;
        private readonly ILoggedInSimpleUserProvider _loggedInSimpleUserProvider;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<SignalRHub> _hubContext;

        public ShoppingService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            IAzureGroupService azureGroupService,
            IAssignmentProfileService assignmentProfileService,
            IAzureUserService azureUserService,
            IEmailService emailService,
            IOptions<Options.Environment> envOptions,
            ILoggedInSimpleUserProvider loggedInSimpleUserProvider,
            ILoggedInUserProvider loggedInUserProvider,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager,
            IHubContext<SignalRHub> hubContext)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _azureGroupService = azureGroupService;
            _assignmentProfileService = assignmentProfileService;
            _azureUserService = azureUserService;
            _emailService = emailService;
            _envOptions = envOptions.Value;
            _loggedInSimpleUserProvider = loggedInSimpleUserProvider;
            _loggedInUserProvider = loggedInUserProvider;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Lists all the applications on the shopping app and do the pagination
        /// </summary>
        /// <param name="pageNumber">The number from which than pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="requestStateFilter">The state of the request by which we filter the applications that need to be shown</param>
        /// <returns>A paged result with all applications paged</returns>
        public async Task<PagedResult<ShoppingApplicationDto>> ListApplicationsPagedAsync(int pageNumber, int pageSize, string? searchTerm, RequestState requestStateFilter)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();

            var privateApps = from app in _applicationDbContext.PrivateApplications
                              where app.IsInShop && app.SubscriptionId == loggedInUser.SubscriptionId
                              join request in (from req in _applicationDbContext.PrivateShoppingRequests where req.IsValid && req.RequesterId == loggedInUser.Id select req)
                              on app.Id equals request.ApplicationId into requests
                              from request in requests.DefaultIfEmpty()
                              select new { app, request };

            List<ShoppingApplicationDto>? privateApplications = (await privateApps.ToListAsync())
                .GroupBy(item => item.app.Id)
                .Select(grouping =>
                {
                    // Get the application of the grouping,
                    // this is the same for each element because they are grouped by the application id
                    PrivateApplication? application = grouping.First().app;
                    // Get the request from the element with the highest request.ModifiedOn
                    var requests = grouping
                        .Where(item => item.request is not null);

                    RequestState lastRequestState = RequestState.NotSet;

                    if (requests.Any())
                    {
                        lastRequestState = requests
                            .OrderByDescending(item => item.request.ModifiedOn)
                            .First()
                            .request
                            .StateId;
                    }

                    return new ShoppingApplicationDto
                    {
                        Description = application.Description,
                        IconBase64 = application.IconBase64,
                        Id = application.Id,
                        Name = application.Name,
                        Publisher = application.Publisher,
                        Version = application.Version,
                        RequestState = lastRequestState,
                        IsPrivate = true
                    };
                })
                .ToList();

            var publicApps = from app in _applicationDbContext.SubscriptionPublicApplications
                             where app.IsInShop && app.SubscriptionId == loggedInUser.SubscriptionId
                             join request in (from req in _applicationDbContext.PublicShoppingRequests where req.IsValid && req.RequesterId == loggedInUser.Id select req)
                             on app.PublicApplicationId equals request.ApplicationId into requests
                             from request in requests.DefaultIfEmpty()
                             select new { app = app.PublicApplication, request };

            List<ShoppingApplicationDto>? subscriptionPublicApplications = (await publicApps.ToListAsync())
                // Group the items by the subscriptionPublicApplication.PublicApplicationId,
                // creating a grouping with the subscriptionPublicApplication.PublicApplicationId as key and the items as elements
                .GroupBy(item => item.app.Id)
                .Select(grouping =>
                {
                    PublicApplication? application = grouping.First().app;
                    var requests = grouping
                        .Where(item => item.request is not null);

                    RequestState lastRequestState = RequestState.NotSet;

                    if (requests.Any())
                    {
                        lastRequestState = requests
                            .OrderByDescending(item => item.request.ModifiedOn)
                            .First()
                            .request
                            .StateId;
                    }

                    return new ShoppingApplicationDto
                    {
                        Description = application.Description,
                        IconBase64 = application.IconBase64,
                        Id = application.Id,
                        Name = application.Name,
                        Publisher = application.Publisher,
                        Version = application.Version,
                        RequestState = lastRequestState,
                        IsPrivate = false
                    };
                }).ToList();

            IEnumerable<ShoppingApplicationDto>? shoppingApplications = privateApplications
                .Concat(subscriptionPublicApplications);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                shoppingApplications = shoppingApplications
                    .Where(application => application.Name.Trim().Contains(searchTerm.Trim(), StringComparison.InvariantCultureIgnoreCase));
            }

            if (requestStateFilter != RequestState.NotSet)
            {
                shoppingApplications = shoppingApplications
                    .Where(application => application.RequestState == requestStateFilter);
            }

            IOrderedQueryable<PrivateShoppingRequest>? privateRequests = _applicationDbContext.PrivateShoppingRequests
                .OrderByDescending(request => request.ResolvedOn);

            IOrderedQueryable<PublicShoppingRequest>? publicRequests = _applicationDbContext.PublicShoppingRequests
                .OrderByDescending(request => request.ResolvedOn);

            return new PagedResult<ShoppingApplicationDto>
            {
                AllItemsCount = shoppingApplications.Count(),
                PageItems = shoppingApplications
                .OrderBy(application => application.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList()
            };
        }

        /// <summary>
        /// Lists all the requests on the Request page of shopping app and do the pagination
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="requestStateFilter">The state of the request by which we filter the applications that need to be shown</param>
        /// <param name="managerId">The id of the manager that gets the request from his subordinates</param>
        /// <returns>A paged result with all requests paged</returns>
        public async Task<PagedResult<ShoppingRequestDto>> ListRequestsPagedAsync(int pageNumber, int pageSize, string? searchTerm, RequestState requestStateFilter, string? managerId)
        {
            Guid subscriptionId = (_loggedInUserProvider.GetLoggedInUser()?.SubscriptionId ?? _loggedInSimpleUserProvider.GetLoggedInUser()?.SubscriptionId)!.Value;

            IQueryable<PrivateShoppingRequest>? privateRequestsQuery = _applicationDbContext.PrivateShoppingRequests
                .Where(request => request.SubscriptionId == subscriptionId);

            AzureUserDto[]? directReports = managerId is null ? null : (await _azureUserService
                .GetUsersDirectReportsAsync(subscriptionId, managerId))
                .ToArray();

            if (requestStateFilter != RequestState.NotSet)
            {
                privateRequestsQuery = privateRequestsQuery
                    .Where(request => request.StateId == requestStateFilter);
            }

            IEnumerable<ShoppingRequestDto>? privateRequests = (await privateRequestsQuery
                .Include(request => request.PrivateApplication)
                .ProjectTo<ShoppingRequestDto>(_mapper.ConfigurationProvider)
                .ToArrayAsync())
                .Select(request =>
                 {
                     request.IsPrivate = true;
                     return request;
                 });

            IQueryable<PublicShoppingRequest>? publicRequestsQuery = _applicationDbContext.PublicShoppingRequests
                .Where(request => request.SubscriptionId == subscriptionId);

            if (requestStateFilter != RequestState.NotSet)
            {
                publicRequestsQuery = publicRequestsQuery
                    .Where(request => request.StateId == requestStateFilter);
            }

            IEnumerable<ShoppingRequestDto>? publicRequests = (await publicRequestsQuery
                .Include(request => request.SubscriptionPublicApplication.PublicApplication)
                .ProjectTo<ShoppingRequestDto>(_mapper.ConfigurationProvider)
                .ToArrayAsync())
                .Select(request =>
                {
                    request.IsPrivate = false;
                    return request;
                });

            IEnumerable<ShoppingRequestDto>? requests = privateRequests
                .Concat(publicRequests);

            if (directReports is not null)
            {
                requests = requests
                    .Where(request => directReports.Any(user => user.Id == request.RequesterId));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                requests = requests
                    .Where(request => request.RequesterName.Trim().Contains(searchTerm.Trim())
                    || request.ApplicationName.Trim().Contains(searchTerm.Trim()));
            }

            return new PagedResult<ShoppingRequestDto>
            {
                AllItemsCount = requests.Count(),
                PageItems = requests
                    .OrderByDescending(request => request.StateId)
                    .ThenByDescending(request => request.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
            };
        }

        /// <summary>
        /// Add a new private application from EndpointAdmin on the Shopping app
        /// </summary>
        /// <param name="shopAddDto">The dto of the application that will be added</param>
        /// <returns>Void</returns>
        public async Task AddPrivateApplicationsAsync(ShopAddDto shopAddDto)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptionId);

            PrivateApplication[] privateApplications = await _applicationDbContext.PrivateApplications
                .Where(privateApplication => privateApplication.SubscriptionId == subscriptionId && 
                    shopAddDto.ApplicationIds.Contains(privateApplication.Id) &&
                    privateApplication.IntuneId != null && 
                    privateApplication.DeploymentStatus != DeploymentStatus.Failed &&
                    privateApplication.DeploymentStatus != DeploymentStatus.InProgress)
                .ToArrayAsync();



            foreach (PrivateApplication privateApp in privateApplications)
            {
                privateApp.IsInShop = true;

                await _notificationService.GenerateSuccessfulShopAddedNotificationsAsync(
                   privateApp.Name,
                   subscriptionId,
                   _loggedInUserProvider.GetLoggedInUser().Id,
                   isForPrivateRepository: true);

                if (privateApp.ShopGroupId is null)
                {
                    AzureGroupDto? group = await _azureGroupService.AddAzureGroupAsync(subscriptionId, shopAddDto.GroupName);
                    privateApp.ShopGroupId = group.Id;

                    if (group is null)
                    {
                        await _notificationService.GenerateFailedShopAddedNotificationsAsync(
                            subscriptionId,
                            privateApp.Name,
                            _loggedInUserProvider.GetLoggedInUser().Id,
                            isForPrivateRepository: true,
                            extraErrorMessage: "The application group failed to be created."
                        );
                    }
                }

                if (shopAddDto.ShouldCreateAssignmentProfile)
                {
                    await CreateAssignmentProfileAsync(subscriptionId, privateApp.ShopGroupId, privateApp.Name);
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Add a new public application from EndpointAdmin on the Shopping app
        /// </summary>
        /// <param name="shopAddDto">The dto of the application that will be added</param>
        /// <returns>Void</returns>
        public async Task AddPublicApplicationsAsync(ShopAddDto shopAddDto)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            Subscription? subscription = await _applicationDbContext.Subscriptions.FindAsync(subscriptionId);

            SubscriptionPublicApplication[] subscriptionPublicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == subscriptionId && shopAddDto.ApplicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .Include(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplication)
                .ToArrayAsync();

            foreach (SubscriptionPublicApplication subscriptionPublicApplication in subscriptionPublicApplications)
            {
                subscriptionPublicApplication.IsInShop = true;

                await _notificationService.GenerateSuccessfulShopAddedNotificationsAsync(
                   subscriptionPublicApplication.PublicApplication.Name,
                   subscriptionId,
                   _loggedInUserProvider.GetLoggedInUser().Id,
                   isForPrivateRepository: false);

                if (subscriptionPublicApplication.ShopGroupId is null)
                {
                    AzureGroupDto? group = await _azureGroupService.AddAzureGroupAsync(subscriptionId, shopAddDto.GroupName);
                    subscriptionPublicApplication.ShopGroupId = group.Id;

                    if (group is null)
                    {
                        await _notificationService.GenerateFailedShopAddedNotificationsAsync(
                            subscriptionId,
                            subscriptionPublicApplication.PublicApplication.Name,
                            _loggedInUserProvider.GetLoggedInUser().Id,
                            isForPrivateRepository: false,
                            extraErrorMessage: "The application group failed to be created.");
                    }      
                }

                if (shopAddDto.ShouldCreateAssignmentProfile)
                {
                    await CreateAssignmentProfileAsync(subscriptionId, subscriptionPublicApplication.ShopGroupId, subscriptionPublicApplication.PublicApplication.Name);
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Delete a private application from the Shopping app
        /// </summary>
        /// <param name="shopAddDto">The dto of the application that will be removed</param>
        /// <returns>Void</returns>
        public async Task RemovePrivateApplicationsAsync(ShopRemoveDto shopAddDto)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            PrivateApplication[]? privateApplications = await _applicationDbContext.PrivateApplications
                .Where(privateApplication => privateApplication.SubscriptionId == subscriptionId && shopAddDto.ApplicationIds.Contains(privateApplication.Id))
                .ToArrayAsync();

            foreach (PrivateApplication? privateApp in privateApplications)
            {
                privateApp.IsInShop = false;

                if (shopAddDto.ShouldDeleteGroup)
                {
                    PrivateShoppingRequest[]? requests = await _applicationDbContext.PrivateShoppingRequests
                        .Where(request => request.PrivateApplication.Id == privateApp.Id)
                        .ToArrayAsync();

                    foreach (PrivateShoppingRequest? request in requests)
                    {
                        request.IsValid = false;
                    }

                    await DeleteGroupAsync(subscriptionId, privateApp.ShopGroupId);
                    privateApp.ShopGroupId = null;
                }

                if (shopAddDto.ShouldDeleteAssignmentProfile)
                {
                    // TODO: Ask how to handle this
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a public application from the Shopping app
        /// </summary>
        /// <param name="shopAddDto">The dto of the application that will be removed</param>
        /// <returns>Void</returns>
        public async Task RemovePublicApplicationsAsync(ShopRemoveDto shopAddDto)
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;
            SubscriptionPublicApplication[]? subscriptionPublicApplications = await _applicationDbContext.SubscriptionPublicApplications
                .Where(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == subscriptionId && shopAddDto.ApplicationIds.Contains(subscriptionPublicApplication.PublicApplicationId))
                .ToArrayAsync();

            foreach (SubscriptionPublicApplication? subscriptionPublicApplication in subscriptionPublicApplications)
            {
                subscriptionPublicApplication.IsInShop = false;

                if (shopAddDto.ShouldDeleteGroup)
                {
                    PublicShoppingRequest[]? requests = await _applicationDbContext.PublicShoppingRequests
                        .Where(request => request.SubscriptionId == subscriptionPublicApplication.SubscriptionId && request.ApplicationId == subscriptionPublicApplication.PublicApplicationId)
                        .ToArrayAsync();

                    foreach (PublicShoppingRequest? request in requests)
                    {
                        request.IsValid = false;
                    }

                    await DeleteGroupAsync(subscriptionId, subscriptionPublicApplication.ShopGroupId);
                    subscriptionPublicApplication.ShopGroupId = null;
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the private application from the Shopping app
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get</param>
        /// <returns>The private application information we want to get</returns>
        public async Task<ShoppingApplicationDetailsDto> GetPrivateApplicationAsync(int applicationId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser()!;
            PrivateApplication? application = await FindPrivateApplicationAsync(loggedInUser.SubscriptionId, applicationId);

            ShoppingApplicationDetailsDto? applicationDto = _mapper.Map<ShoppingApplicationDetailsDto>(application);

            applicationDto.RequestState = (await _applicationDbContext.PrivateShoppingRequests
                .Where(request => request.IsValid &&
                    request.ApplicationId == application.Id &&
                    request.SubscriptionId == loggedInUser.SubscriptionId &&
                    request.RequesterId == loggedInUser.Id)
                .OrderByDescending(request => request.ModifiedOn)
                .FirstOrDefaultAsync()
                )?
                .StateId ?? RequestState.NotSet;

            return applicationDto;
        }

        /// <summary>
        /// Gets the public application from the Shopping app
        /// </summary>
        /// <param name="applicationId">The id of the application we want to get</param>
        /// <returns>The public application information we want to get</returns>
        public async Task<ShoppingApplicationDetailsDto> GetPublicApplicationAsync(int applicationId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser()!;
            SubscriptionPublicApplication? application = await FindPublicApplicationAsync(loggedInUser.SubscriptionId, applicationId);

            ShoppingApplicationDetailsDto? applicationDto = _mapper.Map<ShoppingApplicationDetailsDto>(application.PublicApplication);

            applicationDto.RequestState = (await _applicationDbContext.PublicShoppingRequests
                .Where(request => request.IsValid &&
                    request.ApplicationId == application.PublicApplicationId &&
                    request.SubscriptionId == loggedInUser.SubscriptionId &&
                    request.RequesterId == loggedInUser.Id)
                .OrderByDescending(request => request.ModifiedOn)
                .FirstOrDefaultAsync()
                )?
                .StateId ?? RequestState.NotSet;

            return applicationDto;
        }

        /// <summary>
        /// Handles the request action of a public application from Shopping app
        /// </summary>
        /// <param name="applicationId">The id of the application we want to make a request for</param>
        /// <returns>Void</returns>
        public async Task RequestPublicApplicationAsync(int applicationId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            SubscriptionPublicApplication? application = await FindPublicApplicationAsync(loggedInUser.SubscriptionId, applicationId);

            bool requestInProgress = await _applicationDbContext.PublicShoppingRequests
                .AnyAsync(request => request.IsValid
                    && request.ApplicationId == applicationId
                    && request.SubscriptionId == loggedInUser.SubscriptionId
                    && request.RequesterId == loggedInUser.Id
                    && (request.StateId == RequestState.Pending || request.StateId == RequestState.Accepted)
                );

            if (requestInProgress)
            {
                return;
            }

            PublicShoppingRequest? request = new PublicShoppingRequest
            {
                SubscriptionId = loggedInUser.SubscriptionId,
                ApplicationId = applicationId,
                RequesterId = loggedInUser.Id,
                RequesterName = loggedInUser.Name,
            };

            _applicationDbContext.PublicShoppingRequests.Add(request);
            await _applicationDbContext.SaveChangesAsync();
            await SendRequestEmailAsync(loggedInUser, application.PublicApplication.Name);
        }

        /// <summary>
        /// Handles the request action of a private application from Shopping app
        /// </summary>
        /// <param name="applicationId">The id of the application we want to make a request for</param>
        /// <returns>Void</returns>
        public async Task RequestPrivateApplicationAsync(int applicationId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            PrivateApplication? application = await FindPrivateApplicationAsync(loggedInUser.SubscriptionId, applicationId);

            bool requestInProgress = await _applicationDbContext.PrivateShoppingRequests
                .AnyAsync(request => request.IsValid
                    && request.ApplicationId == applicationId
                    && request.SubscriptionId == loggedInUser.SubscriptionId
                    && request.RequesterId == loggedInUser.Id
                    && (request.StateId == RequestState.Pending || request.StateId == RequestState.Accepted)
                );

            if (requestInProgress)
            {
                return;
            }

            PrivateShoppingRequest? request = new PrivateShoppingRequest
            {
                SubscriptionId = loggedInUser.SubscriptionId,
                ApplicationId = applicationId,
                RequesterId = loggedInUser.Id,
                RequesterName = loggedInUser.Name,
            };

            _applicationDbContext.PrivateShoppingRequests.Add(request);
            await _applicationDbContext.SaveChangesAsync();
            await SendRequestEmailAsync(loggedInUser, application.Name);
        }

        /// <summary>
        /// Sends an email to a manager informing him that one of his subordinates made a request
        /// </summary>
        /// <param name="requester">The author of the request action</param>
        /// <param name="applicationName">The name of the application that we want to make a request for</param>
        /// <returns>Void</returns>
        private async Task SendRequestEmailAsync(SimpleUserDto requester, string applicationName)
        {
            AzureUserDto? manager = await _azureUserService.GetUserManagerAsync(requester.SubscriptionId, requester.Id);

            if (manager is null)
            {
                return;
            }

            string? emailTemplate = EmailResources.ShopRequestTemplate;
            List<Attachment>? attachments = new List<Attachment>();
            attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo)));
            attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.ApprovalsIcon, ImageFormat.Png, nameof(EmailResources.ApprovalsIcon)));
            emailTemplate = emailTemplate.Replace("#logoUrl", _envOptions.BaseUrl);
            emailTemplate = emailTemplate.Replace("#endpointAdminShopUrl", _envOptions.ShopUrl);
            emailTemplate = emailTemplate.Replace("#templateApplicationName", applicationName);
            emailTemplate = emailTemplate.Replace("#templateRequesterName", requester.Name);
            emailTemplate = emailTemplate.Replace("#templateRequesterEmail", requester.Email);
            emailTemplate = emailTemplate.Replace("#templateFirstName", manager.Name);

            EmailDetailsDto? emailDetails = new EmailDetailsDto
            {
                ToEmail = manager.Email,
                ToName = manager.Name,
                Attachments = attachments,
                Subject = ShopRequestEmailSubject,
                HTMLContent = emailTemplate
            };

            await _emailService.SendEmailAsync(emailDetails);
        }

        /// <summary>
        /// Sends an email to a shop requester informing him that one of his requests was resolved.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task SendResponseEmailAsync(ShoppingRequestDto request, bool isAdmin = false)
        {
            var requester = await _azureUserService.GetUserAsync(request.SubscriptionId, request.RequesterId);

            if (requester is null)
            {
                return;
            }

            var resolverType = isAdmin ? ShopRequestResolverTypeAdmin : ShopRequestResolverTypeManager;

            string? emailTemplate = EmailResources.ShopRequestResponseTemplate;
            List<Attachment>? attachments = new List<Attachment>();
            attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.Logo, ImageFormat.Png, nameof(EmailResources.Logo)));

            var requestState = "";

            switch (request.StateId)
            {
                case RequestState.Accepted:
                    requestState = ShopRequestStateAccepted;
                    attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.SuccessIcon, ImageFormat.Png, nameof(EmailResources.SuccessIcon)));
                    emailTemplate = emailTemplate.Replace("#templateIconId", nameof(EmailResources.SuccessIcon));
                    break;
                case RequestState.Rejected:
                    requestState = ShopRequestStateRejected;
                    attachments.Add(ApplicationHelper.GetImageAttachment(EmailResources.FailedIcon, ImageFormat.Png, nameof(EmailResources.FailedIcon)));
                    emailTemplate = emailTemplate.Replace("#templateIconId", nameof(EmailResources.FailedIcon));
                    break;
            }

            emailTemplate = emailTemplate.Replace("#logoUrl", _envOptions.BaseUrl);
            emailTemplate = emailTemplate.Replace("#endpointAdminShopUrl", _envOptions.ShopUrl);
            emailTemplate = emailTemplate.Replace("#templateApplicationName", request.ApplicationName);
            emailTemplate = emailTemplate.Replace("#templateRequestState", requestState);
            emailTemplate = emailTemplate.Replace("#templateFirstName", requester.Name);
            emailTemplate = emailTemplate.Replace("#templateResolverName", request.ResolverName);
            emailTemplate = emailTemplate.Replace("#templateResolverType", resolverType);

            var subject = string.Format(ShopResponseEmailSubject, request.ApplicationName, requestState);

            EmailDetailsDto? emailDetails = new EmailDetailsDto
            {
                ToEmail = requester.Email,
                ToName = requester.Name,
                Attachments = attachments,
                Subject = subject,
                HTMLContent = emailTemplate
            };

            await _emailService.SendEmailAsync(emailDetails);
        }

        /// <summary>
        /// Approves a request for a private application by the Admin of the subscription
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ApprovePrivateRequestAsync(long requestId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            PrivateShoppingRequest? request = await FindPrivateRequestAsync(subscriptionId, requestId);

            if (request is null)
            {
                return;
            }

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            PrivateApplication? application = await _applicationDbContext
                .PrivateApplications
                .FindAsync(request.ApplicationId);
            AzureGroup? group = application?.ShopGroup;

            if (group is null)
            {
                return;
            }

            await _azureGroupService.AddUserToGroupAsync(subscriptionId, group.Id, request.RequesterId);

            request.AdminResolverId = loggedInUser.Id;
            request.StateId = RequestState.Accepted;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestAdminApproved, request.RequesterName, application.Name),
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request), true);
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Approves a request for a public application by the Admin of the subscription
        /// </summary>
        /// <param name="user">The author of the request</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ApprovePublicRequestAsync(long requestId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            PublicShoppingRequest? request = await FindPublicRequestAsync(subscriptionId, requestId);

            if (request is null)
            {
                return;
            }

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            SubscriptionPublicApplication? application = await _applicationDbContext
                .SubscriptionPublicApplications
                .FindAsync(subscriptionId, request.ApplicationId);
            AzureGroup? group = application?.ShopGroup;

            if (group is null)
            {
                return;
            }

            await _azureGroupService.AddUserToGroupAsync(subscriptionId, group.Id, request.RequesterId);

            request.AdminResolverId = loggedInUser.Id;
            request.StateId = RequestState.Accepted;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestAdminApproved, request.RequesterName, application.PublicApplication.Name),
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request), true);
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Rejects a request for a private application by the Admin of the subscription
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task RejectPrivateRequestAsync(long requestId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();

            PrivateShoppingRequest? request = await FindPrivateRequestAsync(loggedInUser.SubscriptionId, requestId);

            if (request is null)
            {
                return;
            }

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            PrivateApplication application = await _applicationDbContext
                .PrivateApplications
                .FindAsync(request.ApplicationId);

            request.AdminResolverId = loggedInUser.Id;
            request.StateId = RequestState.Rejected;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestAdminRejected, request.RequesterName, application.Name),
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request), true);
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Rejects a request for a public application by the Admin of the subscription
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task RejectPublicRequestAsync(long requestId)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            PublicShoppingRequest? request = await FindPublicRequestAsync(loggedInUser.SubscriptionId, requestId);

            if (request is null)
            {
                return;
            }

            if (request.StateId != RequestState.Pending)
            {
                return;
            }
            
            SubscriptionPublicApplication? application = await _applicationDbContext
                .SubscriptionPublicApplications
                .FindAsync(loggedInUser.SubscriptionId, request.ApplicationId);

            request.AdminResolverId = loggedInUser.Id;
            request.StateId = RequestState.Rejected;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestAdminRejected, request.RequesterName, application.PublicApplication.Name),
                author: loggedInUser,
                saveChanges: false);
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request), true);
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Approves a request for a private application by the Manager of the user that made the request
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ManagerApprovePrivateRequestAsync(long requestId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            PrivateShoppingRequest? request = await FindPrivateRequestAsync(subscriptionId, requestId);

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            PrivateApplication? application = await _applicationDbContext
                .PrivateApplications
                .FindAsync(request.ApplicationId);
            AzureGroup? group = application?.ShopGroup;

            if (group is null)
            {
                return;
            }

            await _azureGroupService.AddUserToGroupAsync(subscriptionId, group.Id, request.RequesterId);

            request.ManagerResolverId = loggedInUser.Id;
            request.ManagerResolverName = loggedInUser.Name;
            request.StateId = RequestState.Accepted;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestManagerApproved, loggedInUser.Name, request.RequesterName, application.Name));
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request));
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Approves a request for a public application by the Manager of the user that made the request
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ManagerApprovePublicRequestAsync(long requestId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;
            PublicShoppingRequest? request = await FindPublicRequestAsync(subscriptionId, requestId);

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            SubscriptionPublicApplication? application = await _applicationDbContext
                .SubscriptionPublicApplications
                .FindAsync(subscriptionId, request.ApplicationId);
            AzureGroup? group = application?.ShopGroup;

            if (group is null)
            {
                return;
            }

            await _azureGroupService.AddUserToGroupAsync(subscriptionId, group.Id, request.RequesterId);

            request.ManagerResolverId = loggedInUser.Id;
            request.ManagerResolverName = loggedInUser.Name;
            request.StateId = RequestState.Accepted;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestManagerApproved, loggedInUser.Name, request.RequesterName, application.PublicApplication.Name));
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request));
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Rejects a request for a private application by the Manager of the user that made the request
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ManagerRejectPrivateRequestAsync(long requestId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            PrivateShoppingRequest? request = await FindPrivateRequestAsync(loggedInUser.SubscriptionId, requestId);
            PrivateApplication? application = await _applicationDbContext
                .PrivateApplications
                .FindAsync(request.ApplicationId);

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            request.StateId = RequestState.Rejected;
            request.ManagerResolverName = loggedInUser.Name;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestManagerApproved, loggedInUser.Name, request.RequesterName, application.Name));
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request));
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Rejects a request for a public application by the Manager of the user that made the request
        /// </summary>
        /// <param name="requestId">The id of the request</param>
        /// <returns>Void</returns>
        public async Task ManagerRejectPublicRequestAsync(long requestId)
        {
            SimpleUserDto? loggedInUser = _loggedInSimpleUserProvider.GetLoggedInUser();
            PublicShoppingRequest? request = await FindPublicRequestAsync(loggedInUser.SubscriptionId, requestId);
            SubscriptionPublicApplication? application = await _applicationDbContext
                .SubscriptionPublicApplications
                .FindAsync(loggedInUser.SubscriptionId, request.ApplicationId);

            if (request.StateId != RequestState.Pending)
            {
                return;
            }

            request.StateId = RequestState.Rejected;
            request.ManagerResolverName = loggedInUser.Name;
            request.ResolvedOn = DateTime.UtcNow;

            await _auditLogService.GenerateAuditLogAsync(
                AuditLogCategory.ShopRequests,
                string.Format(AuditLogActions.ShopRequestManagerApproved, loggedInUser.Name, request.RequesterName, application.PublicApplication.Name));
            await _applicationDbContext.SaveChangesAsync();
            await SendResponseEmailAsync(_mapper.Map<ShoppingRequestDto>(request));
            await UpdateRequestStatusAsync();
        }

        /// <summary>
        /// Finds a request for a public application by it's id
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns>The request for the specific public application</returns>
        /// <exception cref="Exception"></exception>
        private async Task<PublicShoppingRequest> FindPublicRequestAsync(Guid subscriptionId, long requestId)
        {
            PublicShoppingRequest? request = await _applicationDbContext.PublicShoppingRequests
                .Include(request => request.AdminResolver)
                .Include(request => request.SubscriptionPublicApplication)
                .Include(request => request.SubscriptionPublicApplication.PublicApplication)
                .FirstOrDefaultAsync(request => request.Id == requestId); ;

            if (request is null || request.SubscriptionId != subscriptionId)
            {
                _log.Error($"Request with the id:{requestId}, does not exist");
                throw new Exception($"Request with the id:{requestId}, does not exist");
            }

            return request;
        }

        /// <summary>
        /// Finds a request for private application by it's id
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="requestId">The id of the request</param>
        /// <returns>The request for the specific private application</returns>
        /// <exception cref="Exception"></exception>
        private async Task<PrivateShoppingRequest> FindPrivateRequestAsync(Guid subscriptionId, long requestId)
        {
            PrivateShoppingRequest? request = await _applicationDbContext.PrivateShoppingRequests
                .Include(request => request.AdminResolver)
                .Include(request => request.PrivateApplication)
                .FirstOrDefaultAsync(request => request.Id == requestId);

            if (request is null || request.SubscriptionId != subscriptionId)
            {
                _log.Error($"Request with the id:{requestId}, does not exist");
                throw new Exception($"Request with the id:{requestId}, does not exist");
            }

            return request;
        }

        /// <summary>
        /// Finds a public application that is deployed, by it's id
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationId">The id of the application we want to find</param>
        /// <returns>The public application we want to find</returns>
        /// <exception cref="Exception"></exception>
        private async Task<SubscriptionPublicApplication> FindPublicApplicationAsync(Guid subscriptionId, int applicationId)
        {
            SubscriptionPublicApplication? application = await _applicationDbContext.SubscriptionPublicApplications
                .Include(subscriptionPublicApplication => subscriptionPublicApplication.PublicApplication)
                .FirstOrDefaultAsync(subscriptionPublicApplication => subscriptionPublicApplication.SubscriptionId == subscriptionId
                    && subscriptionPublicApplication.PublicApplicationId == applicationId
                    && subscriptionPublicApplication.IsInShop);

            if (application is null)
            {
                _log.Error($"Application with the id:{applicationId}, does not exist");
                throw new Exception($"Application with the id:{applicationId}, does not exist");
            }

            return application;
        }

        /// <summary>
        /// Find a private application by it's id
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="applicationId">The id of the application we want to find</param>
        /// <returns>The private application we want to find</returns>
        /// <exception cref="Exception"></exception>
        private async Task<PrivateApplication> FindPrivateApplicationAsync(Guid subscriptionId, int applicationId)
        {
            PrivateApplication? application = await _applicationDbContext.PrivateApplications
                .FirstOrDefaultAsync(application => application.SubscriptionId == subscriptionId
                    && application.Id == applicationId
                    && application.IsInShop);

            if (application is null)
            {
                _log.Error($"Application with the id:{applicationId}, does not exist");
                throw new Exception($"Application with the id:{applicationId}, does not exist");
            }

            return application;
        }

        /// <summary>
        /// Creates automatically an assignment profile when adding an application to the shop
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopGroupId">The if of the azure group included in assignment profile</param>
        /// <param name="applicationName">The name of the application we created an assignment profile for</param>
        /// <returns>Void</returns>
        private async Task CreateAssignmentProfileAsync(Guid subscriptionId, string shopGroupId, string applicationName)
        {
            AzureGroup? azureGroup = await _applicationDbContext.AzureGroups.FindAsync(shopGroupId);

            await _assignmentProfileService.AddAssignmentProfileAsync(new NewAssignmentProfileDto
            {
                Name = string.Format(ShopAssignmentProfileNameFormat, applicationName),
                Groups = new AssignmentProfileGroupDto[]
                {
                    new AssignmentProfileGroupDto
                    {
                        AzureGroupId = new Guid(shopGroupId),
                        DisplayName = azureGroup.Name,
                        AssignmentTypeId = AssignmentType.Available,
                        DeliveryOptimizationPriorityId = DeliveryOptimizationPriority.ContentDownloadInForeground,
                        EndUserNotificationId = EndUserNotification.ShowAllToastNotifications,
                        GroupModeId = GroupMode.Included
                    }
                }
            });
        }

        /// <summary>
        /// Deletes an azure group base on it's id
        /// </summary>
        /// <param name="subscriptionId">The id of the current subscription</param>
        /// <param name="shopGroupId">The if of the azure group included in assignment profile</param>
        /// <returns>Void</returns>
        private async Task DeleteGroupAsync(Guid subscriptionId, string? shopGroupId)
        {
            if (shopGroupId is null)
            {
                return;
            }

            AssignmentProfileGroup[]? assignmentProfileGroups = await _applicationDbContext.AssignmentProfileGroups
                .Where(group => group.AzureGroupId.ToString() == shopGroupId)
                .ToArrayAsync();

            _applicationDbContext.AssignmentProfileGroups
                .RemoveRange(assignmentProfileGroups);

            try
            {
                await _azureGroupService.DeleteGroupAsync(subscriptionId, shopGroupId);
            }
            catch { }
        }

        /// <summary>
        /// Gets all the shop requests of the current subscription and counts them
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetRequestsCountAsync()
        {
            Guid subscriptionId = _loggedInUserProvider.GetLoggedInUser().SubscriptionId;

            var privateShopRequestCount = await _applicationDbContext.PrivateShoppingRequests
                .Where(request => request.StateId == RequestState.Pending && request.SubscriptionId == subscriptionId)
                .CountAsync();

            var publicShopRequestCount = await _applicationDbContext.PublicShoppingRequests
                .Where(request => request.StateId == RequestState.Pending && request.SubscriptionId == subscriptionId)
                .CountAsync();

            return privateShopRequestCount + publicShopRequestCount;
        }

        /// <summary>
        /// Updates the state of the approval
        /// </summary>
        /// <param name="approvalIds">The id of the approval we want to update</param>
        /// <param name="approvalDecision">The decision the user made for the respective approval</param>
        /// <returns></returns>
        private async Task UpdateRequestStatusAsync()
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            Guid subscriptionId = loggedInUser.SubscriptionId;


            var subscriptionUserIds = await _applicationDbContext.SubscriptionUsers
                    .Where(subscriptionUser => subscriptionUser.SubscriptionId == loggedInUser.SubscriptionId)
                    .Select(subscriptionUser => subscriptionUser.ApplicationUserId)
            .ToArrayAsync();

            await _hubContext.Clients.Users(subscriptionUserIds).SendAsync(SignalRMessages.UpdateShopRequestCount);
        }
    }
}