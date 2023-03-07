using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class PrivateRepositoryUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivateRepositoryArchiveFileName",
                table: "PrivateApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "PrivateApplications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PrivateApplications_SubscriptionId",
                table: "PrivateApplications",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateApplications_Subscriptions_SubscriptionId",
                table: "PrivateApplications",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateApplications_Subscriptions_SubscriptionId",
                table: "PrivateApplications");

            migrationBuilder.DropIndex(
                name: "IX_PrivateApplications_SubscriptionId",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "PrivateRepositoryArchiveFileName",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "PrivateApplications");
        }
    }
}
