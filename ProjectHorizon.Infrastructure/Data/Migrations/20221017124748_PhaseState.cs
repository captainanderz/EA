using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class PhaseState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhaseState",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhaseState",
                table: "DeploymentSchedulePrivateApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhaseState",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "PhaseState",
                table: "DeploymentSchedulePrivateApplications");
        }
    }
}
