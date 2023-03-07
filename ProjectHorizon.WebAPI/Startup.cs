using AspNetCoreRateLimit;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ProjectHorizon.ApplicationCore;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.ApplicationCore.Utility;
using ProjectHorizon.Infrastructure;
using ProjectHorizon.TestingSetup;
using ProjectHorizon.WebAPI.Authorization;
using System;

namespace ProjectHorizon.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment currentEnvironment)
        {
            Configuration = configuration;
            CurrentEnvironment = currentEnvironment;
        }

        public IConfiguration Configuration { get; }

        private IWebHostEnvironment CurrentEnvironment { get; set; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="applicationDbContext"></param>
        /// <param name="recurringJobsService"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IApplicationDbContext applicationDbContext,
            IRecurringJobService recurringJobsService,
            ILoggerFactory loggerFactory)
        {
            // https://github.com/huorswords/Microsoft.Extensions.Logging.Log4Net.AspNetCore
            loggerFactory.AddLog4Net();

            app.UseIpRateLimiting();

            app.UseSecurityHeaders(env);

            applicationDbContext.Database.Migrate();
            recurringJobsService.RegisterAll();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectHorizon.WebAPI v1"));
            }


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard();
                endpoints.MapHub<SignalRHub>("/signalr-messages");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLazyResolution();
            services.AddInfrastructure(Configuration);
            services.AddApplicationCore(Configuration);
            services.AddHttpContextAccessor();

            if (CurrentEnvironment.IsDevelopment() || CurrentEnvironment.IsStaging())
            {
                services.AddDbOrchestrator(Configuration);
            }

            services.ConfigureAuthentication(Configuration);

            // for AspNetCoreRateLimit
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            services.AddScoped<IAuthorizationHandler, ActiveRefreshTokenHandler>();

            services.AddControllers(configuration =>
            {
                configuration.Conventions.Add(new RouteTokenTransformerConvention(new HyphenParameterTransformer()));
                configuration.Conventions.Add(new AuthorizationConvention());
            });

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthConstants.AzurePolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(AuthConstants.AzureAuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy(AuthConstants.SimplePolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(HorizonClaimTypes.SubscriptionId);
                });
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: CorsConstants.ShopPolicy,
                    builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string>().Split(";"));
                    });
            });

            services.AddSwaggerGen(configuration =>
            {
                configuration.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectHorizon.WebAPI", Version = "v1" });
            });

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            services.AddRouting(configuration =>
            {
                configuration.LowercaseUrls = true;
                configuration.LowercaseQueryStrings = true;
            });

            services.AddSignalR().AddAzureSignalR(Configuration["AzureSignalR:ConnectionString"]);

            services.ConfigureHangfire(Configuration);

            if (Configuration["ApplicationInsights:ConnectionString"] != null)
            {
                services.AddApplicationInsightsTelemetry();
            }
        }
    }
}