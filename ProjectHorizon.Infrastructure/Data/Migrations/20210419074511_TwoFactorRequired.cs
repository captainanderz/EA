using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class TwoFactorRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorRequired",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorRequired",
                table: "AspNetUsers");
        }
    }
}
