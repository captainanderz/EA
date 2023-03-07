using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class PrivateRepoDeployStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeployedVersion",
                table: "PrivateApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeploymentStatus",
                table: "PrivateApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntuneId",
                table: "PrivateApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeployedVersion",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "DeploymentStatus",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "IntuneId",
                table: "PrivateApplications");
        }
    }
}
