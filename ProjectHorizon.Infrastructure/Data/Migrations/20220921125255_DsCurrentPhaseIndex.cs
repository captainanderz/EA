using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DsCurrentPhaseIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedules_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentSchedules");

            migrationBuilder.DropIndex(
                name: "IX_DeploymentSchedules_CurrentPhaseId",
                table: "DeploymentSchedules");

            migrationBuilder.DropColumn(
                name: "CurrentPhaseId",
                table: "DeploymentSchedules");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPhaseIndex",
                table: "DeploymentSchedules",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPhaseIndex",
                table: "DeploymentSchedules");

            migrationBuilder.AddColumn<long>(
                name: "CurrentPhaseId",
                table: "DeploymentSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedules_CurrentPhaseId",
                table: "DeploymentSchedules",
                column: "CurrentPhaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedules_DeploymentSchedulePhases_CurrentPhaseId",
                table: "DeploymentSchedules",
                column: "CurrentPhaseId",
                principalTable: "DeploymentSchedulePhases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
