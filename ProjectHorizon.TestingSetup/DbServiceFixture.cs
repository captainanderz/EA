using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.TestingSetup
{
    public abstract class DbServiceFixture : IAsyncLifetime, IDisposable
    {
        protected DbServiceFixture()
        {
            Services = new ServiceCollection()
                .AddTestingSetup()
                .BuildServiceProvider();

            //this is here for initializing hangfire
            Services.GetRequiredService<IGlobalConfiguration>();
        }

        public IServiceProvider Services { get; private set; }

        public abstract Task InitializeAsync();

        public async Task DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)Services)?.Dispose();
            }

            Services = null;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (Services is not null)
            {
                await ((IAsyncDisposable)Services).DisposeAsync().ConfigureAwait(false);
            }

            Services = null;
        }
    }
}