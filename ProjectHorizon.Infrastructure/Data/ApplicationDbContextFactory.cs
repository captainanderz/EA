using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace ProjectHorizon.Infrastructure.Data
{
    // Needed this because ApplicationDbContext is not in the startup WebAPI project
    // and this was the only way I could run Add-Migration commands (otherwise they were failing);
    // Add-Migration looks for a class implementing IDesignTimeDbContextFactory, like this one
    class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        private readonly string connectionString =
            "Server=tcp:project-horizon-dev-db.database.windows.net,1433;" +
            "Initial Catalog=project-horizon-dev-db;" +
            "Persist Security Info=False;" +
            "User ID=project-horizon-dev-db;" +
            "Password=eCA9uzDx6bakm8wv;" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout=30;";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<ApplicationDbContext>? optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder
                .UseSqlServer(connectionString)
                .UseLazyLoadingProxies();

            return new ApplicationDbContext(optionsBuilder.Options, Options.Create(new OperationalStoreOptions()));
        }
    }
}
