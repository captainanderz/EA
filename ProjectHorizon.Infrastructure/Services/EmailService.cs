using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationCore.Options.SendGrid _sendGridOptions;

        public EmailService(IOptions<ApplicationCore.Options.SendGrid> sendGridOptions)
        {
            _sendGridOptions = sendGridOptions.Value;
        }

        public async Task<bool> SendEmailAsync(EmailDetailsDto emailDetails)
        {
            SendGridClient? client = new SendGridClient(_sendGridOptions.ApiKey);

            if (string.IsNullOrEmpty(emailDetails.FromEmail))
            {
                emailDetails.FromEmail = _sendGridOptions.DefaultFromEmail;
                emailDetails.FromName = _sendGridOptions.DefaultFromName;
            }

            EmailAddress? fromEmailAddress = new EmailAddress(emailDetails.FromEmail, emailDetails.FromName);
            EmailAddress? toEmailAddress = new EmailAddress(emailDetails.ToEmail, emailDetails.ToName);

            string? plainTextContent = StripHtmlTags(emailDetails.HTMLContent);

            SendGridMessage? email = MailHelper.CreateSingleEmail(fromEmailAddress, toEmailAddress, emailDetails.Subject,
                plainTextContent, emailDetails.HTMLContent);

            if (emailDetails.Attachments?.Count > 0)
            {
                email.AddAttachments(emailDetails.Attachments);
            }

            Response? response = await client.SendEmailAsync(email);

            return response.StatusCode == HttpStatusCode.Accepted;
        }

        private static string StripHtmlTags(string input)
        {
            string? output = Regex.Replace(input, "<[^>]*(>|$)", string.Empty);

            return Regex.Replace(output, @"[\s\r\n]+", " ").Trim();
        }
    }
}
