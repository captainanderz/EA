using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ShopGroupPrefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShopGroupPrefix",
                table: "Subscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopGroupPrefix",
                table: "Subscriptions");
        }
    }
}
