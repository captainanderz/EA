using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class ShopRequestForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdminApproverId",
                table: "PublicShoppingRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminApproverId",
                table: "PrivateShoppingRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicShoppingRequests_AdminApproverId",
                table: "PublicShoppingRequests",
                column: "AdminApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateShoppingRequests_AdminApproverId",
                table: "PrivateShoppingRequests",
                column: "AdminApproverId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PrivateShoppingRequests",
                column: "AdminApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PublicShoppingRequests",
                column: "AdminApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PublicShoppingRequests_AdminApproverId",
                table: "PublicShoppingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrivateShoppingRequests_AdminApproverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.AlterColumn<int>(
                name: "AdminApproverId",
                table: "PublicShoppingRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AdminApproverId",
                table: "PrivateShoppingRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
