using ProjectHorizon.ApplicationCore.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectHorizon.UnitTests.ApplicationCore.Services
{
    public class ApprovalServiceTests : IClassFixture<ApprovalServiceFixture>
    {
        private readonly IApprovalService _approvalService;

        public ApprovalServiceTests(ApprovalServiceFixture fixture)
        {
            _approvalService = fixture.GetApprovalService();
        }

        [Theory]
        [InlineData(0, 0, 1, ApprovalServiceFixture.SubscriptionWithNoApprovals)]
        [InlineData(1, 1, 1, ApprovalServiceFixture.SubscriptionWithOneActiveApproval)]
        [InlineData(1, 0, 2, ApprovalServiceFixture.SubscriptionWithOneActiveApproval)]
        [InlineData(1, 0, 3, ApprovalServiceFixture.SubscriptionWithOneActiveApproval)]
        public async Task ListApprovalsPagedAsync_ReturnsCorrectData(int expectedAllItemsCount, int expectedPagedItemsCount, int pageNumber,
            string subscriptionId)
        {
            //act
            ProjectHorizon.ApplicationCore.DTOs.PagedResult<ProjectHorizon.ApplicationCore.DTOs.ApprovalDto>? result = await _approvalService.ListApprovalsPagedAsync(pageNumber, 10);

            //assert
            Assert.Equal(expectedAllItemsCount, result.AllItemsCount);
            Assert.Equal(expectedPagedItemsCount, result.PageItems.Count());
        }

        [Theory]
        [InlineData(0, ApprovalServiceFixture.SubscriptionWithNoApprovals)]
        [InlineData(1, ApprovalServiceFixture.SubscriptionWithOneActiveApproval)]
        public async Task GetApprovalsCountAsync_ReturnsCorrectData(int expected, string id)
        {
            //act
            int result = await _approvalService.GetApprovalsCountAsync();

            //assert
            Assert.Equal(expected, result);
        }
    }
}