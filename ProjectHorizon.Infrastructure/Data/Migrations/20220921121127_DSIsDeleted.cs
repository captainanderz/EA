using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DSIsDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DeploymentSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DeploymentSchedules");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
