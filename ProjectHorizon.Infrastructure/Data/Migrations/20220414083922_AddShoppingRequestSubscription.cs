using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class AddShoppingRequestSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "PrivateShoppingRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PrivateShoppingRequests_SubscriptionId",
                table: "PrivateShoppingRequests",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateShoppingRequests_Subscriptions_SubscriptionId",
                table: "PrivateShoppingRequests",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateShoppingRequests_Subscriptions_SubscriptionId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrivateShoppingRequests_SubscriptionId",
                table: "PrivateShoppingRequests");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "PrivateShoppingRequests");
        }
    }
}
