using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class AzureGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShopGroupId",
                table: "SubscriptionPublicApplications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedOn",
                table: "PublicShoppingRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedOn",
                table: "PrivateShoppingRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ShopGroupId",
                table: "PrivateApplications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AzureGroups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPublicApplications_ShopGroupId",
                table: "SubscriptionPublicApplications",
                column: "ShopGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateApplications_ShopGroupId",
                table: "PrivateApplications",
                column: "ShopGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateApplications_AzureGroups_ShopGroupId",
                table: "PrivateApplications",
                column: "ShopGroupId",
                principalTable: "AzureGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPublicApplications_AzureGroups_ShopGroupId",
                table: "SubscriptionPublicApplications",
                column: "ShopGroupId",
                principalTable: "AzureGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateApplications_AzureGroups_ShopGroupId",
                table: "PrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPublicApplications_AzureGroups_ShopGroupId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropTable(
                name: "AzureGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPublicApplications_ShopGroupId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_PrivateApplications_ShopGroupId",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "ShopGroupId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "ResolvedOn",
                table: "PublicShoppingRequests");

            migrationBuilder.DropColumn(
                name: "ResolvedOn",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropColumn(
                name: "ShopGroupId",
                table: "PrivateApplications");
        }
    }
}
