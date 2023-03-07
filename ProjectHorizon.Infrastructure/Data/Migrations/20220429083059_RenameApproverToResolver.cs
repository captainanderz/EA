using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class RenameApproverToResolver : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicShoppingRequests_AspNetUsers_AdminApproverId",
                table: "PublicShoppingRequests");

            migrationBuilder.RenameColumn(
                name: "ManagerApproverName",
                table: "PublicShoppingRequests",
                newName: "ManagerResolverName");

            migrationBuilder.RenameColumn(
                name: "ManagerApproverId",
                table: "PublicShoppingRequests",
                newName: "ManagerResolverId");

            migrationBuilder.RenameColumn(
                name: "AdminApproverId",
                table: "PublicShoppingRequests",
                newName: "AdminResolverId");

            migrationBuilder.RenameIndex(
                name: "IX_PublicShoppingRequests_AdminApproverId",
                table: "PublicShoppingRequests",
                newName: "IX_PublicShoppingRequests_AdminResolverId");

            migrationBuilder.RenameColumn(
                name: "ManagerApproverName",
                table: "PrivateShoppingRequests",
                newName: "ManagerResolverName");

            migrationBuilder.RenameColumn(
                name: "ManagerApproverId",
                table: "PrivateShoppingRequests",
                newName: "ManagerResolverId");

            migrationBuilder.RenameColumn(
                name: "AdminApproverId",
                table: "PrivateShoppingRequests",
                newName: "AdminResolverId");

            migrationBuilder.RenameIndex(
                name: "IX_PrivateShoppingRequests_AdminApproverId",
                table: "PrivateShoppingRequests",
                newName: "IX_PrivateShoppingRequests_AdminResolverId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateShoppingRequests_AspNetUsers_AdminResolverId",
                table: "PrivateShoppingRequests",
                column: "AdminResolverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicShoppingRequests_AspNetUsers_AdminResolverId",
                table: "PublicShoppingRequests",
                column: "AdminResolverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateShoppingRequests_AspNetUsers_AdminResolverId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicShoppingRequests_AspNetUsers_AdminResolverId",
                table: "PublicShoppingRequests");

            migrationBuilder.RenameColumn(
                name: "ManagerResolverName",
                table: "PublicShoppingRequests",
                newName: "ManagerApproverName");

            migrationBuilder.RenameColumn(
                name: "ManagerResolverId",
                table: "PublicShoppingRequests",
                newName: "ManagerApproverId");

            migrationBuilder.RenameColumn(
                name: "AdminResolverId",
                table: "PublicShoppingRequests",
                newName: "AdminApproverId");

            migrationBuilder.RenameIndex(
                name: "IX_PublicShoppingRequests_AdminResolverId",
                table: "PublicShoppingRequests",
                newName: "IX_PublicShoppingRequests_AdminApproverId");

            migrationBuilder.RenameColumn(
                name: "ManagerResolverName",
                table: "PrivateShoppingRequests",
                newName: "ManagerApproverName");

            migrationBuilder.RenameColumn(
                name: "ManagerResolverId",
                table: "PrivateShoppingRequests",
                newName: "ManagerApproverId");

            migrationBuilder.RenameColumn(
                name: "AdminResolverId",
                table: "PrivateShoppingRequests",
                newName: "AdminApproverId");

            migrationBuilder.RenameIndex(
                name: "IX_PrivateShoppingRequests_AdminResolverId",
                table: "PrivateShoppingRequests",
                newName: "IX_PrivateShoppingRequests_AdminApproverId");

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
    }
}
