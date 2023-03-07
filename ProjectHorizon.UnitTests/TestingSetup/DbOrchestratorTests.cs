using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.TestingSetup;
using ProjectHorizon.TestingSetup.Orchestrator;
using System.Threading.Tasks;

namespace ProjectHorizon.UnitTests.TestingSetup
{
    /// <summary>
    /// This class can be used to interact with the orchestrator. Running the test tests here will make changes to the testing environment.
    /// </summary>
    public class DbOrchestratorTests
    {
        private readonly DbOrchestrator _orchestrator;

        public DbOrchestratorTests()
        {
            ServiceProvider? services = new ServiceCollection()
                .AddTestingSetup()
                .BuildServiceProvider();

            _orchestrator = services.GetRequiredService<DbOrchestrator>();
        }

        // [Fact]
        public async Task RunEnsureHasOwnDbAsync()
        {
            await _orchestrator.EnsureHasOwnDbAsync();
        }

        // [Fact]
        public async Task RunMaintenance()
        {
            await _orchestrator.MaintenanceAsync();
        }
    }
}