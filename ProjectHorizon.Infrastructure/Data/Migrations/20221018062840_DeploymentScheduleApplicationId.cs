using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DeploymentScheduleApplicationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentScheduleSubscriptionPublicApplications",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentSchedulePrivateApplications",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "DeploymentSchedulePrivateApplications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentScheduleSubscriptionPublicApplications",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentSchedulePrivateApplications",
                table: "DeploymentSchedulePrivateApplications",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentScheduleSubscriptionPublicApplications_SubscriptionId_ApplicationId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                columns: new[] { "SubscriptionId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedulePrivateApplications_ApplicationId",
                table: "DeploymentSchedulePrivateApplications",
                column: "ApplicationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentScheduleSubscriptionPublicApplications",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentScheduleSubscriptionPublicApplications_SubscriptionId_ApplicationId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentSchedulePrivateApplications",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentSchedulePrivateApplications_ApplicationId",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentScheduleSubscriptionPublicApplications",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                columns: new[] { "SubscriptionId", "ApplicationId", "Type" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentSchedulePrivateApplications",
                table: "DeploymentSchedulePrivateApplications",
                columns: new[] { "ApplicationId", "Type" });
        }
    }
}
