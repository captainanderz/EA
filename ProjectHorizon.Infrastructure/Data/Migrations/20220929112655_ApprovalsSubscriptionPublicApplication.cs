using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ApprovalsSubscriptionPublicApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Approvals_SubscriptionId",
                table: "Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_SubscriptionId_PublicApplicationId",
                table: "Approvals",
                columns: new[] { "SubscriptionId", "PublicApplicationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_SubscriptionPublicApplications_SubscriptionId_PublicApplicationId",
                table: "Approvals",
                columns: new[] { "SubscriptionId", "PublicApplicationId" },
                principalTable: "SubscriptionPublicApplications",
                principalColumns: new[] { "SubscriptionId", "PublicApplicationId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_SubscriptionPublicApplications_SubscriptionId_PublicApplicationId",
                table: "Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_SubscriptionId_PublicApplicationId",
                table: "Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_SubscriptionId",
                table: "Approvals",
                column: "SubscriptionId");
        }
    }
}
