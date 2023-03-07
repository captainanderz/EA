using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class CurrentPhase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentScheduleSubscriptionPublicApplications_CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "CurrentPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedulePrivateApplications_CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications",
                column: "CurrentPhaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications",
                column: "CurrentPhaseId",
                principalTable: "DeploymentSchedulePhases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "CurrentPhaseId",
                principalTable: "DeploymentSchedulePhases",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentScheduleSubscriptionPublicApplications_CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentSchedulePrivateApplications_CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropColumn(
                name: "CurrentPhaseId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "CurrentPhaseId",
                table: "DeploymentSchedulePrivateApplications");
        }
    }
}
