using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Services;
using ProjectHorizon.ApplicationCore.Services.Notifications;
using ProjectHorizon.ApplicationCore.Utility;
using System;
using System.Reflection;

namespace ProjectHorizon.ApplicationCore
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddScoped<IAssignmentProfileService, AssignmentProfileService>();
            services.AddScoped<IPublicApplicationService, PublicApplicationService>();
            services.AddScoped<IPrivateApplicationService, PrivateApplicationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IApprovalService, ApprovalService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<ILoggedInUserProvider, LoggedInUserProvider>();
            services.AddScoped<ITermsService, TermsService>();
            services.AddScoped<IShoppingService, ShoppingService>();
            services.AddScoped<ILoggedInSimpleUserProvider, LoggedInSimpleUserProvider>();
            services.AddScoped<IDeploymentScheduleService, DeploymentScheduleService>();
            services.AddScoped<IDeploymentScheduleJobService, DeploymentScheduleService>();

            services.AddSingleton<IUserIdProvider, UserIdProvider>();

            services.Configure<AzureStorageInformation>(configuration.GetSection(nameof(AzureStorageInformation)));
            services.Configure<Options.Environment>(configuration.GetSection(nameof(Options.Environment)));
            services.Configure<JwtAuthentication>(configuration.GetSection(nameof(JwtAuthentication)));
            services.Configure<GraphConfigEncryption>(configuration.GetSection(nameof(GraphConfigEncryption)));
            services.AddOptions<RecurringJobs>()
                .Bind(configuration.GetSection(nameof(RecurringJobs)))
                .ValidateDataAnnotations();
            services.Configure<Options.SendGrid>(configuration.GetSection(nameof(Options.SendGrid)));
            services.Configure<Billing>(configuration.GetSection(nameof(Billing)));
            services.Configure<ApplicationInformation>(configuration.GetSection(nameof(ApplicationInformation)));
            services.Configure<DeploymentScheduleOptions>(configuration.GetSection(nameof(DeploymentScheduleOptions)));

            return services;
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            return services.AddTransient(
                typeof(Lazy<>),
                typeof(LazilyResolved<>));
        }
    }
}