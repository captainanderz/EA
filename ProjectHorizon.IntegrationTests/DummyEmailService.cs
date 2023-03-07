using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Threading.Tasks;

namespace ProjectHorizon.IntegrationTests
{
    class DummyEmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(EmailDetailsDto emailDetails)
        {
            if (emailDetails == null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
