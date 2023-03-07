using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Data
{
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>, IApplicationDbContext, IDataProtectionKeyContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        public DbSet<Approval> Approvals { get; set; }

        public DbSet<AssignmentProfileGroup> AssignmentProfileGroups { get; set; }

        public DbSet<AssignmentProfile> AssignmentProfiles { get; set; }

        public DbSet<DeploymentSchedule> DeploymentSchedules { get; set; }

        public DbSet<DeploymentSchedulePhase> DeploymentSchedulePhases { get; set; }

        public DbSet<DeploymentSchedulePrivateApplication> DeploymentSchedulePrivateApplications { get; set; }

        public DbSet<DeploymentScheduleSubscriptionPublicApplication> DeploymentScheduleSubscriptionPublicApplications { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public DbSet<GraphConfig> GraphConfigs { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<NotificationSetting> NotificationSettings { get; set; }

        public DbSet<PrivateApplication> PrivateApplications { get; set; }

        public DbSet<PublicApplication> PublicApplications { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<SubscriptionPublicApplication> SubscriptionPublicApplications { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<SubscriptionUser> SubscriptionUsers { get; set; }

        public DbSet<UserInvitation> UserInvitations { get; set; }

        public DbSet<PrivateShoppingRequest> PrivateShoppingRequests { get; set; }

        public DbSet<PublicShoppingRequest> PublicShoppingRequests { get; set; }

        public DbSet<AzureGroup> AzureGroups { get; set; }

        public DbSet<SubscriptionConsent> SubscriptionConsents { get; set; }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (EntityEntry<BaseEntity>? entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = DateTime.UtcNow;
                        entry.Entity.ModifiedOn = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedOn = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);
        }
    }
}