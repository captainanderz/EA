using Hangfire;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectHorizon.TestingSetup.Orchestrator.Data;
using ProjectHorizon.TestingSetup.Orchestrator.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectHorizon.TestingSetup.Orchestrator
{
    public class DbOrchestrator
    {
        private const int _dbsToHaveReady = 10;
        private const int _ageInDaysOfVeryOldDb = 2;
        private const int _ageInMinutesOfMaxTestingTime = 10;

        private readonly OrchestratorDbContext _context;
        private readonly DbCommands _dbCommands;

        public DbOrchestrator(OrchestratorDbContext context, DbCommands dbCommands)
        {
            _context = context;
            _dbCommands = dbCommands;
        }

        public async Task EnsureHasOwnDbAsync()
        {
            string? connectionString = _context.Database.GetConnectionString();
            string? databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

            if (await _dbCommands.DatabaseExistsAsync(databaseName))
            {
                return;
            }

            await _dbCommands.CreateBasicDbAsync(databaseName);

            string? createTablesScript = _context.Database.GenerateCreateScript();
            string? createTableWithRemovedGo = Regex.Replace(createTablesScript, "\\sGO\\s", "");

            try
            {
                await _context.Database.ExecuteSqlRawAsync(createTableWithRemovedGo);
            }
            catch (Exception)
            {
                await _context.Database.EnsureDeletedAsync();
            }
        }

        /// <summary>
        /// This needs to be called from time to time to ensure there is a pool of dbs ready for testing
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 5 * 10)]
        public async Task MaintenanceAsync()
        {
            await RefreshVeryOldDbsAsync();
            await RefreshRequiredDbsAsync();
            await CreateRequiredDbsAsync();
            await DestroyRequiredDbsAsync();
        }

        /// <summary>
        /// Cleans up potential leaks that happened during unexpected service shutdown.
        /// Controlled by <see cref="_ageInDaysOfVeryOldDb"/>
        /// </summary>
        private async Task RefreshVeryOldDbsAsync()
        {
            IQueryable<DbStatus>? dbsToRefresh = _context.QueryWithLock
                .Where(db => db.DateTime.AddDays(_ageInDaysOfVeryOldDb) < DateTimeOffset.UtcNow);

            await RefreshDbsAsync(dbsToRefresh);
        }

        /// <summary>
        /// Cleans up dbs that are done testing, or have gotten stuck in a test.
        /// Controlled by <see cref="_ageInMinutesOfMaxTestingTime"/>
        /// </summary>
        private async Task RefreshRequiredDbsAsync()
        {
            IQueryable<DbStatus>? dbsToRefresh = _context.QueryWithLock
                .Where(db => db.State == State.TestingDone
                             || db.State == State.TestingInProgress && db.DateTime.AddMinutes(_ageInMinutesOfMaxTestingTime) < DateTimeOffset.UtcNow);

            await RefreshDbsAsync(dbsToRefresh);
        }

        /// <summary>
        /// Controlled by <see cref="_dbsToHaveReady"/> 
        /// </summary>
        private async Task CreateRequiredDbsAsync()
        {
            int numberOfReadyDbs = await _context.Dbs
                .Where(db => db.State == State.ReadyForTesting)
                .CountAsync();

            for (int i = 0; i < _dbsToHaveReady - numberOfReadyDbs; i++)
            {
                DbStatus? status = new DbStatus { State = State.Creating };
                _context.Add(status);
                await _context.SaveChangesAsync();

                await _dbCommands.CreateBasicDbAsync(status.DbName);
                await _dbCommands.MigrateAsync(status.DbName);

                status.State = State.ReadyForTesting;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Controlled by <see cref="_dbsToHaveReady"/> 
        /// </summary>
        private async Task DestroyRequiredDbsAsync()
        {
            List<DbStatus>? readyToTestDbs = await _context.Dbs
                .OrderBy(db => db.DateTime)
                .Where(db => db.State == State.ReadyForTesting)
                .ToListAsync();

            await DropDbsAsync(readyToTestDbs.Take(readyToTestDbs.Count - _dbsToHaveReady));

            List<DbStatus>? creatingDbs = await _context.Dbs
                .Where(db => db.State == State.Creating && db.DateTime < DateTimeOffset.UtcNow.AddMinutes(5))
                .ToListAsync();

            await DropDbsAsync(creatingDbs);
        }

        private async Task DropDbsAsync(IEnumerable<DbStatus> dbs)
        {
            foreach (DbStatus? db in dbs)
            {
                await _dbCommands.DestroyAsync(db.DbName);

                _context.Remove(db);
                await _context.SaveChangesAsync();
            }
        }

        private async Task RefreshDbsAsync(IQueryable<DbStatus> query)
        {
            while (true)
            {
                await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
                DbStatus? db = await query.FirstOrDefaultAsync();

                if (db == null)
                {
                    await transaction.CommitAsync();
                    break;
                }

                db.State = State.Creating;
                await _context.SaveChangesAsync();

                await _dbCommands.ClearDbAsync(db.DbName);
                await _dbCommands.MigrateAsync(db.DbName);

                db.State = State.ReadyForTesting;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
        }
    }
}