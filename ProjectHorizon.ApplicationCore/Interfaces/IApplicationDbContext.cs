using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjectHorizon.ApplicationCore.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IApplicationDbContext
    {
        DatabaseFacade Database { get; }

        DbSet<AssignmentProfileGroup> AssignmentProfileGroups { get; set; }

        DbSet<AssignmentProfile> AssignmentProfiles { get; set; }

        DbSet<DeploymentSchedule> DeploymentSchedules { get; set; }

        DbSet<DeploymentSchedulePhase> DeploymentSchedulePhases { get; set; }

        DbSet<DeploymentSchedulePrivateApplication> DeploymentSchedulePrivateApplications { get; set; }

        DbSet<DeploymentScheduleSubscriptionPublicApplication> DeploymentScheduleSubscriptionPublicApplications { get; set; }

        DbSet<PublicApplication> PublicApplications { get; set; }

        DbSet<PrivateApplication> PrivateApplications { get; set; }

        DbSet<Subscription> Subscriptions { get; set; }

        DbSet<SubscriptionUser> SubscriptionUsers { get; set; }

        DbSet<GraphConfig> GraphConfigs { get; set; }

        DbSet<SubscriptionPublicApplication> SubscriptionPublicApplications { get; set; }

        DbSet<Approval> Approvals { get; set; }

        DbSet<Notification> Notifications { get; set; }

        DbSet<NotificationSetting> NotificationSettings { get; set; }

        DbSet<UserInvitation> UserInvitations { get; set; }

        DbSet<AuditLog> AuditLogs { get; set; }

        DbSet<ApplicationUser> Users { get; set; }

        DbSet<RefreshToken> RefreshTokens { get; set; }

        DbSet<PrivateShoppingRequest> PrivateShoppingRequests { get; set; }

        DbSet<PublicShoppingRequest> PublicShoppingRequests { get; set; }

        DbSet<AzureGroup> AzureGroups { get; set; }

        DbSet<SubscriptionConsent> SubscriptionConsents { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());

        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    }
}
