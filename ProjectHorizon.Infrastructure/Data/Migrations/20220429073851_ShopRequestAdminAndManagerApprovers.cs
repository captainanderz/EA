using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ShopRequestAdminAndManagerApprovers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AzureUserName",
                table: "PublicShoppingRequests",
                newName: "RequesterName");

            migrationBuilder.RenameColumn(
                name: "AzureUserId",
                table: "PublicShoppingRequests",
                newName: "RequesterId");

            migrationBuilder.RenameColumn(
                name: "AzureUserName",
                table: "PrivateShoppingRequests",
                newName: "RequesterName");

            migrationBuilder.RenameColumn(
                name: "AzureUserId",
                table: "PrivateShoppingRequests",
                newName: "RequesterId");

            migrationBuilder.AddColumn<int>(
                name: "AdminApproverId",
                table: "PublicShoppingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerApproverId",
                table: "PublicShoppingRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminApproverId",
                table: "PrivateShoppingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerApproverId",
                table: "PrivateShoppingRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminApproverId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropColumn(
                name: "ManagerApproverId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropColumn(
                name: "AdminApproverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropColumn(
                name: "ManagerApproverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.RenameColumn(
                name: "RequesterName",
                table: "PublicShoppingRequests",
                newName: "AzureUserName");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "PublicShoppingRequests",
                newName: "AzureUserId");

            migrationBuilder.RenameColumn(
                name: "RequesterName",
                table: "PrivateShoppingRequests",
                newName: "AzureUserName");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "PrivateShoppingRequests",
                newName: "AzureUserId");
        }
    }
}
