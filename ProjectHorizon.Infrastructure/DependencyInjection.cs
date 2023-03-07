using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Services;
using ProjectHorizon.Infrastructure.Data;
using ProjectHorizon.Infrastructure.Services;
using System;

namespace ProjectHorizon.Infrastructure
{
    public static class DependencyInjection
    {
        private static readonly ILoggerFactory consoleLoggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options => options
                .UseLoggerFactory(consoleLoggerFactory)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .UseLazyLoadingProxies()
                .UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            );

            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
            services.AddScoped<IDeployIntunewinService, DeployIntunewinService>();
            services.AddScoped<IAzureBlobService, AzureBlobService>();
            services.AddScoped<IIntuneConverterService, IntuneConverterService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IGraphConfigService, GraphConfigService>();
            services.AddScoped<IEconomicService, EconomicService>();
            services.AddScoped<IFarPayService, FarPayService>();
            services.AddScoped<IAzureGroupService, AzureGroupService>();
            services.AddScoped<IGraphAssignmentService, GraphAssignmentService>();
            services.AddScoped<IAzureUserService, AzureUserService>();
            services.AddScoped<IDeploymentScheduleService, DeploymentScheduleService>();
            services.AddScoped<IAzureOrganizationService, AzureOrganizationService>();

            services.AddHttpClient<IEconomicService, EconomicService>(client =>
            {
                string economicUrl = configuration["Economic:Url"];
                if (!string.IsNullOrEmpty(economicUrl))
                {
                    client.BaseAddress = new Uri(economicUrl);
                    client.DefaultRequestHeaders.Add(BillingRequestHeaders.EconomicXAppSecretToken, configuration["Economic:AppSecretToken"]);
                    client.DefaultRequestHeaders.Add(BillingRequestHeaders.EconomicXAgreementGrantToken, configuration["Economic:AgreementGrantToken"]);
                }
            });

            services.AddHttpClient<IFarPayService, FarPayService>(client =>
            {
                string farPayUrl = configuration["FarPay:Url"];
                if (!string.IsNullOrEmpty(farPayUrl))
                {
                    client.BaseAddress = new Uri(farPayUrl);
                    client.DefaultRequestHeaders.Add(BillingRequestHeaders.FarPayXApiKey, configuration["FarPay:XApiKey"]);
                }
            });

            services.AddTransient<IBackgroundJobService, BackgroundJobService>();
            services.AddTransient<IRecurringJobService, RecurringJobService>();

            services.AddIdentityCore<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();

            return services;
        }
    }
}