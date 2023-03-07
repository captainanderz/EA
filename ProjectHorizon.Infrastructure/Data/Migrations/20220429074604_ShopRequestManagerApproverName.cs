using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ShopRequestManagerApproverName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagerApproverName",
                table: "PublicShoppingRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerApproverName",
                table: "PrivateShoppingRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerApproverName",
                table: "PublicShoppingRequests");

            migrationBuilder.DropColumn(
                name: "ManagerApproverName",
                table: "PrivateShoppingRequests");
        }
    }
}
