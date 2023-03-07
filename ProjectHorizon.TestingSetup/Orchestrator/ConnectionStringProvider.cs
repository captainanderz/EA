using Hangfire;
using Microsoft.EntityFrameworkCore;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup.Orchestrator.Data;
using ProjectHorizon.TestingSetup.Orchestrator.Internal;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectHorizon.TestingSetup.Orchestrator
{
    public class ConnectionStringProvider : IAsyncDisposable
    {
        private readonly DbOrchestrator _orchestrator;
        private readonly OrchestratorDbContext _context;
        private readonly DbCommands _dbCommands;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly SemaphoreSlim _disposeSemaphore;
        private DbStatus dbStatus;

        public ConnectionStringProvider(
            DbOrchestrator orchestrator,
            OrchestratorDbContext context,
            DbCommands dbCommands,
            IBackgroundJobService backgroundJobService)
        {
            _orchestrator = orchestrator;
            _context = context;
            _dbCommands = dbCommands;
            _backgroundJobService = backgroundJobService;

            _disposeSemaphore = new SemaphoreSlim(1);
        }

        public async ValueTask DisposeAsync()
        {
            await _disposeSemaphore.WaitAsync();

            if (dbStatus == null)
            {
                _disposeSemaphore.Release();
                return;
            }

            try
            {
                await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

                DbStatus? readAgainStatus = await _context.QueryWithLock
                    .FirstOrDefaultAsync(db => db.Id == dbStatus.Id);

                if (readAgainStatus != null)
                {
                    readAgainStatus.State = State.TestingDone;
                    await _context.SaveChangesAsync();
                    dbStatus = null;
                }

                await transaction.CommitAsync();
            }
            finally
            {
                _disposeSemaphore.Release();
            }

            ScheduleMaintenance();
        }

        public string GetConnectionString()
        {
            if (dbStatus != null)
            {
                throw new Exception($"{nameof(GetConnectionString)} can only be called once.");
            }

            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = _context.Database.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                dbStatus = _context.QueryWithLock
                    .OrderBy(db => db.DateTime)
                    .FirstOrDefault(db => db.State == State.ReadyForTesting);

                if (dbStatus != null)
                {
                    dbStatus.State = State.TestingInProgress;
                    _context.SaveChanges();
                }

                transaction.Commit();
            }

            dbStatus ??= GetNewDb();

            return _dbCommands.FormatToConnectionString(dbStatus.DbName);
        }

        private DbStatus GetNewDb()
        {
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = _context.Database.BeginTransaction(IsolationLevel.RepeatableRead);

            DbStatus? dbNeedingRefresh = _context.QueryWithLock
                .OrderBy(db => db.DateTime)
                .FirstOrDefault(db => db.State == State.TestingDone);

            if (dbNeedingRefresh != null)
            {
                dbNeedingRefresh.State = State.Creating;
                _context.SaveChanges();

                _dbCommands.ClearDb(dbNeedingRefresh.DbName);
                _dbCommands.Migrate(dbNeedingRefresh.DbName);

                dbNeedingRefresh.State = State.ReadyForTesting;
                _context.SaveChanges();
            }

            transaction.Commit();

            return dbNeedingRefresh ?? CreateNewDb();
        }

        private DbStatus CreateNewDb()
        {
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = _context.Database.BeginTransaction(IsolationLevel.RepeatableRead);

            DbStatus? newDb = new DbStatus { State = State.Creating };
            _context.Add(newDb);
            _context.SaveChanges();

            _dbCommands.CreateBasicDb(newDb.DbName);
            _dbCommands.Migrate(newDb.DbName);

            newDb.State = State.TestingInProgress;
            _context.SaveChanges();

            transaction.Commit();

            return newDb;
        }

        private void ScheduleMaintenance()
        {
            Hangfire.Storage.IMonitoringApi? jobMonitor = JobStorage.Current.GetMonitoringApi();

            bool isInProgress = jobMonitor.ProcessingJobs(0, 10)
                .Any(j => j.Value.Job.Method.Name == nameof(DbOrchestrator.MaintenanceAsync));
            if (isInProgress)
            {
                return;
            }

            bool isScheduled = jobMonitor.EnqueuedJobs("default", 0, 10)
                .Any(j => j.Value.Job.Method.Name == nameof(DbOrchestrator.MaintenanceAsync));
            if (isScheduled)
            {
                return;
            }

            _backgroundJobService.Enqueue(() => _orchestrator.MaintenanceAsync());
        }
    }
}