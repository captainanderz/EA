using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ArchiveFileName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveFileName",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ArchiveFileName",
                table: "DeploymentSchedulePrivateApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveFileName",
                table: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "ArchiveFileName",
                table: "DeploymentSchedulePrivateApplications");
        }
    }
}
