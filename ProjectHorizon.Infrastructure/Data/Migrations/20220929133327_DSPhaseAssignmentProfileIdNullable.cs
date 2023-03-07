using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DSPhaseAssignmentProfileIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.AlterColumn<long>(
                name: "AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.AlterColumn<long>(
                name: "AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
