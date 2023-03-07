using System.IO;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DownloadInfoDto
    {
        public string ContentName { get; init; }

        public string ContentType { get; init; }

        public Task<Stream> Content { get; init; }
    }
}
