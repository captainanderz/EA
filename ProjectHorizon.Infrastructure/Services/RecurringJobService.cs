using Hangfire;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    public class RecurringJobService : IRecurringJobService
    {
        private readonly RecurringJobs _recurringJobs;

        public RecurringJobService(IOptions<RecurringJobs> recurringJobs)
        {
            _recurringJobs = recurringJobs.Value;
        }

        public void RegisterAll()
        {
            RegisterSendUnreadNotificationEmails();
            RegisterRemoveOldRefreshTokens();
            RegisterUpdateDeviceCount();
        }

        public void RegisterJob(string jobId, Expression<Func<Task>> job, string cronExpression)
        {
            RecurringJob.AddOrUpdate(jobId, job, cronExpression);
        }

        public void RemoveJob(string jobId)
        {
            RecurringJob.RemoveIfExists(jobId);
        }

        private void RegisterSendUnreadNotificationEmails()
        {
            RecurringJob.AddOrUpdate<INotificationService>(
                "send-unread-notification-emails",
                ns => ns.SendUnreadNotificationEmailsAsync(),
                Cron.Daily(_recurringJobs.SendUnreadNotificationEmailsHour));
        }

        private void RegisterRemoveOldRefreshTokens()
        {
            RecurringJob.AddOrUpdate<IAuthService>(
                "remove-old-refresh-tokens",
                aus => aus.RemoveOldRefreshTokensAsync(),
                Cron.Daily(12));
        }

        private void RegisterUpdateDeviceCount()
        {
            RecurringJob.AddOrUpdate<IDeployIntunewinService>(
                "update-device-count",
                service => service.UpdateDevicesCountForAllSubscriptionsAsync(),
                Cron.Daily(_recurringJobs.UpdateDeviceCountHour));
        }
    }
}