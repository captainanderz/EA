using ProjectHorizon.ApplicationCore.DTOs;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailDetailsDto emailDetails);
    }
}