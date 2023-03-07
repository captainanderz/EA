using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class AddSubscriptionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "AssignmentProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DeliveryOptimizationPriorityId",
                table: "AssignmentProfileGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndUserNotificationId",
                table: "AssignmentProfileGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentProfiles_SubscriptionId",
                table: "AssignmentProfiles",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentProfiles_Subscriptions_SubscriptionId",
                table: "AssignmentProfiles",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentProfiles_Subscriptions_SubscriptionId",
                table: "AssignmentProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentProfiles_SubscriptionId",
                table: "AssignmentProfiles");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "AssignmentProfiles");

            migrationBuilder.DropColumn(
                name: "DeliveryOptimizationPriorityId",
                table: "AssignmentProfileGroups");

            migrationBuilder.DropColumn(
                name: "EndUserNotificationId",
                table: "AssignmentProfileGroups");
        }
    }
}
