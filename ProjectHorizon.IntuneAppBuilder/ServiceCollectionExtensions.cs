using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectHorizon.IntuneAppBuilder.Services;
using System.Net.Http;

namespace ProjectHorizon.IntuneAppBuilder
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers required services.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddIntuneAppBuilder(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddHttpClient();
            services.TryAddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());
            services.TryAddTransient<IIntuneAppPublishingService, IntuneAppPublishingService>();
            services.TryAddTransient<IIntuneAppPackagingService, IntuneAppPackagingService>();

            return services;
        }
    }
}
