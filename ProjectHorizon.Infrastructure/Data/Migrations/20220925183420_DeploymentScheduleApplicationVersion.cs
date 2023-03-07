using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DeploymentScheduleApplicationVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "DeploymentSchedulePrivateApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DeploymentSchedulePrivateApplications");
        }
    }
}
