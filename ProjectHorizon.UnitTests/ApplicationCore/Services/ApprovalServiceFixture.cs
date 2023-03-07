using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Threading.Tasks;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class ApprovalServiceFixture : DbServiceFixture
    {
        public const string SubscriptionWithNoApprovals = "711bd26b-1211-4b8f-8872-c6af43cda759";
        public const string SubscriptionWithOneActiveApproval = "a75d6a7d-e240-4825-a343-8a8fe397dad6";

        public override async Task InitializeAsync()
        {
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();

            context.Subscriptions.AddRange(
                new Subscription
                {
                    Name = "Subscription With No Approvals",
                    Id = Guid.Parse(SubscriptionWithNoApprovals),
                    CompanyName = "",
                    Email = "",
                    City = "",
                    Country = "",
                    ZipCode = "",
                    VatNumber = "",
                    State = "",
                    CustomerNumber = ""
                },
                new Subscription
                {
                    Name = "Subscription With One Active Approval",
                    Id = Guid.Parse(SubscriptionWithOneActiveApproval),
                    CompanyName = "",
                    Email = "",
                    City = "",
                    Country = "",
                    ZipCode = "",
                    VatNumber = "",
                    State = "",
                    CustomerNumber = ""
                }
            );

            PublicApplication? publicApplication1 = new PublicApplication
            {
                Name = "PublicApp1",
                Version = "1.0"
            };
            PublicApplication? publicApplication2 = new PublicApplication
            {
                Name = "PublicApp2",
                Version = "1.0"
            };
            context.PublicApplications.AddRange(publicApplication1, publicApplication2);

            context.SubscriptionPublicApplications.Add(
                new SubscriptionPublicApplication
                {
                    DeployedVersion = "1.0",
                    SubscriptionId = Guid.Parse(SubscriptionWithOneActiveApproval),
                    PublicApplication = publicApplication1
                }
            );

            context.Approvals.AddRange(
                new Approval
                {
                    PublicApplication = publicApplication1,
                    SubscriptionId = Guid.Parse(SubscriptionWithOneActiveApproval),
                    IsActive = true,
                    UserDecision = ApprovalDecision.Approved
                },
                new Approval
                {
                    PublicApplication = publicApplication2,
                    SubscriptionId = Guid.Parse(SubscriptionWithOneActiveApproval),
                    IsActive = false,
                    UserDecision = ApprovalDecision.Rejected
                }
            );

            await context.SaveChangesAsync();
        }

        public IApprovalService GetApprovalService() =>
            Services.GetRequiredService<IApprovalService>();
    }
}