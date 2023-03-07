using Microsoft.Extensions.DependencyInjection;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.TestingSetup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class SubscriptionServiceFixture : DbServiceFixture
    {
        private readonly static Guid _validSubscriptionId = Guid.NewGuid();

        public override async Task InitializeAsync()
        {
            IApplicationDbContext? context = Services.GetRequiredService<IApplicationDbContext>();
            context.Subscriptions.AddRange(
                new Subscription
                {
                    Id = _validSubscriptionId,
                    Name = "Zwable-Subscription-1",
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
                    Id = Guid.NewGuid(),
                    Name = "Zwable-Subscription-2",
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
                    Id = Guid.NewGuid(),
                    Name = "Zwable-Subscription-3",
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
                    Id = Guid.NewGuid(),
                    Name = "SubscriptionAutoUpdate",
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
                    Id = Guid.NewGuid(),
                    Name = "SubscriptionManualApprove",
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

            await context.SaveChangesAsync();
        }

        internal ISubscriptionService GetSubscriptionService()
            => Services.GetRequiredService<ISubscriptionService>();

        public readonly static IEnumerable<object[]> FilterSubscriptionsByNameAsyncData = new List<object[]>
        {
            new object[]
            {
                "zwable", //subscription name
                3, //expected number of subscriptions filtered
            },
            new object[]
            {
                "empty",
                0,
            },
        };

        public readonly static IEnumerable<object[]> GetSubscriptionAsyncData = new List<object[]>
        {
            new object[]
            {
                _validSubscriptionId, // subscription id
            },
        };

        public readonly static IEnumerable<object[]> GetSubscriptionAsync_ThrowsInvalidOperationException_Data = new List<object[]>
        {
            new object[]
            {
                Guid.NewGuid(), // subscription id
            },
        };
    }
}