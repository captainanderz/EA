using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ApplicationArchitectureField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Architecture",
                table: "PublicApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Architecture",
                table: "PrivateApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Architecture",
                table: "PublicApplications");

            migrationBuilder.DropColumn(
                name: "Architecture",
                table: "PrivateApplications");
        }
    }
}
