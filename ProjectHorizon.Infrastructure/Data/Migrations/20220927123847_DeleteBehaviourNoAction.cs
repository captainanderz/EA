using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DeleteBehaviourNoAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePrivateApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePrivateApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
