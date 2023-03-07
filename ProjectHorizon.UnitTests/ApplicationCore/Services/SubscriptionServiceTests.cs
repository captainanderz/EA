using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Linq;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class SubscriptionServiceTests : IClassFixture<SubscriptionServiceFixture>
    {
        private readonly ISubscriptionService subscriptionService;

        public SubscriptionServiceTests(SubscriptionServiceFixture subscriptionServiceFixture)
        {
            subscriptionService = subscriptionServiceFixture.GetSubscriptionService();
        }

        [Theory]
        [MemberData(
            memberName: nameof(SubscriptionServiceFixture.FilterSubscriptionsByNameAsyncData),
            MemberType = typeof(SubscriptionServiceFixture)
        )]
        public async void FilterSubscriptionsByNameAsync(string subName, int expectedNumberOfSubscriptionsFiltered)
        {
            // Arrange
            //Check MemberData

            // Act
            System.Collections.Generic.IEnumerable<ProjectHorizon.ApplicationCore.DTOs.SubscriptionDto>? actual = await subscriptionService.FilterSubscriptionsByNameAsync(subName);

            // Assert
            Assert.StrictEqual(expectedNumberOfSubscriptionsFiltered, actual.Count());
        }


        [Theory]
        [MemberData(
            memberName: nameof(SubscriptionServiceFixture.GetSubscriptionAsyncData),
            MemberType = typeof(SubscriptionServiceFixture)
        )]
        public async void GetSubscriptionAsync(Guid subscriptionId)
        {
            // Arrange
            //Check MemberData

            // Act
            ProjectHorizon.ApplicationCore.DTOs.SubscriptionDto? actual = await subscriptionService.GetSubscriptionAsync(subscriptionId);

            //Assert
            Assert.NotNull(actual);
            Assert.StrictEqual(subscriptionId, actual.Id);
        }

        [Theory]
        [MemberData(
            memberName: nameof(SubscriptionServiceFixture.GetSubscriptionAsync_ThrowsInvalidOperationException_Data),
            MemberType = typeof(SubscriptionServiceFixture)
        )]
        public async void GetSubscriptionAsync_ThrowsInvalidOperationException(Guid subscriptionId)
        {
            // Arrange
            //Check MemberData

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => subscriptionService.GetSubscriptionAsync(subscriptionId)
            );
        }
    }
}
