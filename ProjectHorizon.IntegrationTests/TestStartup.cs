using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Configuration;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Services;
using ProjectHorizon.ApplicationCore.Services.Notifications;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.Infrastructure;
using ProjectHorizon.WebAPI;
using System;
using System.Reflection;

namespace ProjectHorizon.IntegrationTests
{
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddInfrastructure(Configuration);

            Type t = typeof(AutoMapperProfile);
            services.AddAutoMapper(Assembly.GetAssembly(t));

            services.ConfigureAuthentication(Configuration);
            services.Configure<GraphConfigEncryption>(Configuration.GetSection(nameof(GraphConfigEncryption)));

            services.AddScoped<ILoggedInUserProvider, LoggedInUserProvider>();
            services.AddScoped<IPublicApplicationService, PublicApplicationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, DummyEmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ILoggedInUserProvider, LoggedInUserProvider>();
            services.AddScoped<IAssignmentProfileService, AssignmentProfileService>();
            services.AddScoped<IDeploymentScheduleJobService, DeploymentScheduleService>();

            services.AddControllers(configuration =>
            {
                configuration.Conventions.Add(new RouteTokenTransformerConvention(new HyphenParameterTransformer()));
            })
            .AddApplicationPart(Assembly.Load("ProjectHorizon.WebAPI"))
            .AddControllersAsServices();

            services.AddRouting(configuration =>
            {
                configuration.LowercaseUrls = true;
                configuration.LowercaseQueryStrings = true;
            });

            services.AddSignalR();

            services.ConfigureHangfire(Configuration);
            services.Configure<AzureStorageInformation>(Configuration.GetSection(nameof(AzureStorageInformation)));
            services.Configure<JwtAuthentication>(Configuration.GetSection(nameof(JwtAuthentication)));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SignalRHub>("/signalr-messages");
            });
        }
    }
}