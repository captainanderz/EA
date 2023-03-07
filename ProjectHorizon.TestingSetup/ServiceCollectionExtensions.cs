using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProjectHorizon.ApplicationCore;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using ProjectHorizon.ApplicationCore.Services.Signals;
using ProjectHorizon.Infrastructure;
using ProjectHorizon.Infrastructure.Data;
using ProjectHorizon.TestingSetup.Orchestrator;
using ProjectHorizon.TestingSetup.Orchestrator.Internal;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectHorizon.TestingSetup
{
    public static class ServiceCollectionExtensions
    {
        private static IConfiguration Configuration => new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        public static IServiceCollection AddTestingSetup(this IServiceCollection services) {
            services
                .AddApplicationServices()
                .AddTestingExternalServices(Configuration)
                .AddDbOrchestrator(Configuration)
                .AddTestingDb();

            services.Configure<GraphConfigEncryption>(Configuration.GetSection(nameof(GraphConfigEncryption)));
            services.Configure<Billing>(Configuration.GetSection(nameof(Billing)));
            services.Configure<ApplicationInformation>(Configuration.GetSection(nameof(ApplicationInformation)));
            services.Configure<DeploymentScheduleOptions>(Configuration.GetSection(nameof(DeploymentScheduleOptions)));

            return services;
        }

        public static IServiceCollection AddDbOrchestrator(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<OrchestratorDbContext>(options => options
                .UseSqlServer(string.Format(configuration.GetConnectionString("AutomatedTesting")), o => o.CommandTimeout(90)));

            services.AddTransient<DbOrchestrator>();
            services.AddTransient<ConnectionStringProvider>();
            services.AddTransient<DbCommands>();

            return services;
        }

        private static IServiceCollection AddApplicationServices(this IServiceCollection services) => services
            .AddApplicationCore(Configuration)
            .AddInfrastructure(Configuration)
            .ConfigureHangfire(Configuration);

        private static IServiceCollection AddTestingExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.AddScoped<IDeployIntunewinService>(_ => null);
            services.AddScoped<ILoggedInUserProvider, LoggedInUserProviderMock>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            Mock<IAzureBlobService>? mockAzureBlobService = new Mock<IAzureBlobService>();

            mockAzureBlobService
                .Setup(abs => abs.UploadPackageOnPrivateRepositoryAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockAzureBlobService
                .Setup(abs => abs.GetDownloadUriForPackageUsingSas(null, It.IsAny<Guid?>()))
                .Throws<ArgumentNullException>();

            Mock<IClientProxy>? mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(cp => cp.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            IHubContext<SignalRHub>? mockMessageHubContext = Mock.Of<IHubContext<SignalRHub>>(hb => hb.Clients.All == mockClientProxy.Object);
            services.AddScoped(_ => mockAzureBlobService.Object);
            services.AddScoped(_ => mockMessageHubContext);

            return services;
        }

        private static IServiceCollection AddTestingDb(this IServiceCollection services)
        {
            ServiceDescriptor? dbContextOptionsDescriptor = services.SingleOrDefault(s =>
                s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            services.Remove(dbContextOptionsDescriptor);

            services.AddDbContext<ApplicationDbContext>((sp, options) => options
                .UseLazyLoadingProxies()
                .UseSqlServer(
                    sp.GetRequiredService<ConnectionStringProvider>().GetConnectionString(),
                    b =>
                    {
                        b.EnableRetryOnFailure(5);
                        b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    })
            );

            return services;
        }
    }
}