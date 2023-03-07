using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.IntuneAppBuilder.Domain;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ProjectHorizon.IntuneAppBuilder.Services
{
    /// <summary>
    /// Functionality for interacting with Intune APIs.
    /// </summary>
    public interface IIntuneAppPublishingService
    {
        Task<MobileLobApp> PublishAsync(IntuneAppPackage package, PackageInfo packageInfo);

        Task<MobileLobApp> PublishUpdateAsync(IntuneAppPackage package, PackageInfo packageInfo);

        Task RemoveAsync(string intuneAppId);

        Task<MobileLobApp> GetApplicationInfoAsync(string intuneAppId);

        Task<int> GetManagedDevicesCountAsync();

        Task<int> GetComanagedDevicesCountAsync();
    }
}
