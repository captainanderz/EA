using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ProjectHorizon.Infrastructure.Data;
using System.Threading.Tasks;

namespace ProjectHorizon.TestingSetup.Orchestrator.Internal
{
    public class DbCommands
    {
        private readonly string _connectionStringTemplate;

        public DbCommands(OrchestratorDbContext context, IConfiguration configuration)
        {
            _connectionStringTemplate = configuration.GetConnectionString("AutomatedTestingTemplate");
        }

        public string FormatToConnectionString(string databaseName) => string.Format(_connectionStringTemplate, databaseName);

        public async Task<bool> DatabaseExistsAsync(string databaseName)
        {
            await using SqlConnection? connection = new SqlConnection(FormatToConnectionString("master"));
            connection.Open();

            SqlCommand? command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(name) FROM sys.databases WHERE name=@Db";
            command.Parameters.AddWithValue("@Db", databaseName);

            int result = (int?)await command.ExecuteScalarAsync() ?? 0;

            return result == 1;
        }

        public async Task ClearDbAsync(string databaseName)
        {
            (SqlConnection connection, SqlCommand dropFksCommand, SqlCommand dropTablesCommand) = ClearDbImpl(databaseName);

            try
            {
                await dropFksCommand.ExecuteNonQueryAsync();
                await dropTablesCommand.ExecuteNonQueryAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public void ClearDb(string databaseName)
        {
            (SqlConnection connection, SqlCommand dropFksCommand, SqlCommand dropTablesCommand) = ClearDbImpl(databaseName);

            try
            {
                dropFksCommand.ExecuteNonQuery();
                dropTablesCommand.ExecuteNonQuery();
            }
            finally
            {
                connection.Dispose();
            }
        }

        private (SqlConnection connection, SqlCommand dropFksCommand, SqlCommand dropTablesCommand) ClearDbImpl(string databaseName)
        {
            SqlConnection? connection = new SqlConnection(FormatToConnectionString(databaseName));
            connection.Open();

            SqlCommand? dropFks = connection.CreateCommand();
            dropFks.CommandText = @"
while(exists(select 1 from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_TYPE='FOREIGN KEY'))
begin
    declare @sql nvarchar(2000)
    SELECT TOP 1 @sql=('ALTER TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + '] DROP CONSTRAINT [' + CONSTRAINT_NAME + ']')
    FROM information_schema.table_constraints
    WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
    exec (@sql)
end";
            SqlCommand? dropTables = connection.CreateCommand();
            dropTables.CommandText = @"
while(exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME != '__MigrationHistory' AND TABLE_TYPE = 'BASE TABLE'))
begin
    declare @sql nvarchar(2000)
    SELECT TOP 1 @sql=('DROP TABLE ' + TABLE_SCHEMA + '.[' + TABLE_NAME + ']')
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME != '__MigrationHistory' AND TABLE_TYPE = 'BASE TABLE'
    exec (@sql)
end";

            return (connection, dropFks, dropTables);
        }

        public Task CreateBasicDbAsync(string databaseName) => ExecuteCommandOnMasterDbAsync($"CREATE DATABASE [{databaseName}] (EDITION = 'basic')");

        public void CreateBasicDb(string databaseName) => ExecuteCommandOnMasterDb($"CREATE DATABASE [{databaseName}] (EDITION = 'basic')");

        public Task DestroyAsync(string databaseName) => ExecuteCommandOnMasterDbAsync($"DROP DATABASE [{databaseName}]");

        public async Task MigrateAsync(string databaseName)
        {
            await using ApplicationDbContext? context = GetContextForMigrate(databaseName);
            await context.Database.MigrateAsync();
        }

        public void Migrate(string databaseName)
        {
            using ApplicationDbContext? context = GetContextForMigrate(databaseName);
            context.Database.Migrate();
        }

        public ApplicationDbContext GetContextForMigrate(string databaseName)
        {
            DbContextOptions<ApplicationDbContext>? contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(FormatToConnectionString(databaseName))
                .Options;

            IOptions<OperationalStoreOptions>? operationalStoreOptions = Options.Create(new OperationalStoreOptions());

            return new ApplicationDbContext(contextOptions, operationalStoreOptions);
        }

        private async Task ExecuteCommandOnMasterDbAsync(string commandText)
        {
            await using SqlConnection? connection = new SqlConnection(FormatToConnectionString("master"));
            connection.Open();
            SqlCommand? command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        private void ExecuteCommandOnMasterDb(string commandText)
        {
            using SqlConnection? connection = new SqlConnection(FormatToConnectionString("master"));
            connection.Open();
            SqlCommand? command = connection.CreateCommand();
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }
    }
}