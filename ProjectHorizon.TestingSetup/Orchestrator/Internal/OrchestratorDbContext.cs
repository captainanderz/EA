using Microsoft.EntityFrameworkCore;
using ProjectHorizon.TestingSetup.Orchestrator.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectHorizon.TestingSetup.Orchestrator.Internal
{
    public class OrchestratorDbContext : DbContext
    {
        public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : base(options)
        {
        }

        public DbSet<DbStatus> Dbs { get; set; }

        public IQueryable<DbStatus> QueryWithLock => Dbs
            .FromSqlRaw("SELECT * FROM dbo.Dbs WITH (TABLOCKX, HOLDLOCK)");

        public override int SaveChanges()
        {
            UpdateDateTime();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            UpdateDateTime();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateDateTime()
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<DbStatus>? entry in ChangeTracker.Entries<DbStatus>())
            {
                entry.Entity.DateTime = DateTimeOffset.UtcNow;
            }
        }
    }
}