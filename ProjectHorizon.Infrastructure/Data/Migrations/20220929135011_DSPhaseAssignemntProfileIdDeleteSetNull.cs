using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DSPhaseAssignemntProfileIdDeleteSetNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id");
        }
    }
}
