using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class FixPublicShoppingRequestFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublicShoppingRequests_SubscriptionPublicApplications_ApplicationId_SubscriptionId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SubscriptionPublicApplications_PublicApplicationId_SubscriptionId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_PublicShoppingRequests_ApplicationId_SubscriptionId",
                table: "PublicShoppingRequests");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPublicApplications_PublicApplicationId",
                table: "SubscriptionPublicApplications",
                column: "PublicApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicShoppingRequests_SubscriptionId_ApplicationId",
                table: "PublicShoppingRequests",
                columns: new[] { "SubscriptionId", "ApplicationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PublicShoppingRequests_SubscriptionPublicApplications_SubscriptionId_ApplicationId",
                table: "PublicShoppingRequests",
                columns: new[] { "SubscriptionId", "ApplicationId" },
                principalTable: "SubscriptionPublicApplications",
                principalColumns: new[] { "SubscriptionId", "PublicApplicationId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublicShoppingRequests_SubscriptionPublicApplications_SubscriptionId_ApplicationId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPublicApplications_PublicApplicationId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_PublicShoppingRequests_SubscriptionId_ApplicationId",
                table: "PublicShoppingRequests");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SubscriptionPublicApplications_PublicApplicationId_SubscriptionId",
                table: "SubscriptionPublicApplications",
                columns: new[] { "PublicApplicationId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicShoppingRequests_ApplicationId_SubscriptionId",
                table: "PublicShoppingRequests",
                columns: new[] { "ApplicationId", "SubscriptionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PublicShoppingRequests_SubscriptionPublicApplications_ApplicationId_SubscriptionId",
                table: "PublicShoppingRequests",
                columns: new[] { "ApplicationId", "SubscriptionId" },
                principalTable: "SubscriptionPublicApplications",
                principalColumns: new[] { "PublicApplicationId", "SubscriptionId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
