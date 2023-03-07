using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ShopRequestUserName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AzureUserName",
                table: "PublicShoppingRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AzureUserName",
                table: "PrivateShoppingRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AzureUserName",
                table: "PublicShoppingRequests");

            migrationBuilder.DropColumn(
                name: "AzureUserName",
                table: "PrivateShoppingRequests");
        }
    }
}
