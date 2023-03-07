using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class BaseServiceFixture : DbServiceFixture
    {
        public readonly string ValidSuperAdminUserId = Guid.NewGuid().ToString();
        public readonly string ValidAdministratorUserId = Guid.NewGuid().ToString();
        public readonly string ValidContributorUserId = Guid.NewGuid().ToString();
        public readonly string ValidReaderUserId = Guid.NewGuid().ToString();

        public override async Task InitializeAsync()
        {
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();
            UserManager<ApplicationUser>? userManager = Services.GetRequiredService<UserManager<ApplicationUser>>();

            IdentityResult? identityResult = await userManager.CreateAsync(new()
            {
                Id = ValidSuperAdminUserId,
                UserName = "Base" + UserRole.SuperAdmin + ValidSuperAdminUserId,
                FirstName = "Base" + UserRole.SuperAdmin + "first name",
                LastName = "Base" + UserRole.SuperAdmin + "last name",
                IsSuperAdmin = true,
            });

            Assert.True(identityResult.Succeeded);

            identityResult = await userManager.CreateAsync(new()
            {
                Id = ValidAdministratorUserId,
                UserName = "Base" + UserRole.Administrator + ValidAdministratorUserId,
                FirstName = "Base" + UserRole.Administrator + "first name",
                LastName = "Base" + UserRole.Administrator + "last name",
                IsSuperAdmin = false,
            });

            Assert.True(identityResult.Succeeded);

            identityResult = await userManager.CreateAsync(new()
            {
                Id = ValidContributorUserId,
                UserName = "Base" + UserRole.Contributor + ValidContributorUserId,
                FirstName = "Base" + UserRole.Contributor + "first name",
                LastName = "Base" + UserRole.Contributor + "last name",
                IsSuperAdmin = false,
            });

            Assert.True(identityResult.Succeeded);

            identityResult = await userManager.CreateAsync(new()
            {
                Id = ValidReaderUserId,
                UserName = "Base" + UserRole.Reader + ValidReaderUserId,
                FirstName = "Base" + UserRole.Reader + "first name",
                LastName = "Base" + UserRole.Reader + "last name",
                IsSuperAdmin = false,
            });

            Assert.True(identityResult.Succeeded);

            await context.SaveChangesAsync();
        }
    }
}