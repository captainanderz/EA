using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IIntuneConverterService
    {
        Task ConvertToIntuneFormatAsync(string archiveFilePath);

        Task<string> ConvertToIntuneAndUpload(string archiveFileName, string iconFileName = null);
    }
}
