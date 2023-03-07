using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class Shopping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPublicApplications_PublicApplicationId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.AddColumn<bool>(
                name: "IsInShop",
                table: "SubscriptionPublicApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInShop",
                table: "PrivateApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SubscriptionPublicApplications_PublicApplicationId_SubscriptionId",
                table: "SubscriptionPublicApplications",
                columns: new[] { "PublicApplicationId", "SubscriptionId" });

            migrationBuilder.CreateTable(
                name: "PrivateShoppingRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AzureUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateShoppingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateShoppingRequests_PrivateApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PrivateApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicShoppingRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AzureUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicShoppingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicShoppingRequests_SubscriptionPublicApplications_ApplicationId_SubscriptionId",
                        columns: x => new { x.ApplicationId, x.SubscriptionId },
                        principalTable: "SubscriptionPublicApplications",
                        principalColumns: new[] { "PublicApplicationId", "SubscriptionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateShoppingRequests_ApplicationId",
                table: "PrivateShoppingRequests",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicShoppingRequests_ApplicationId_SubscriptionId",
                table: "PublicShoppingRequests",
                columns: new[] { "ApplicationId", "SubscriptionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateShoppingRequests");

            migrationBuilder.DropTable(
                name: "PublicShoppingRequests");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SubscriptionPublicApplications_PublicApplicationId_SubscriptionId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "IsInShop",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "IsInShop",
                table: "PrivateApplications");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPublicApplications_PublicApplicationId",
                table: "SubscriptionPublicApplications",
                column: "PublicApplicationId");
        }
    }
}
