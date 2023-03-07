using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class AssignmentProfilesAzureGroupIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignmentProfileGroups",
                table: "AssignmentProfileGroups");

            migrationBuilder.AlterColumn<Guid>(
                name: "AzureGroupId",
                table: "AssignmentProfileGroups",
                type: "uniqueidentifier",
                maxLength: 68,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldMaxLength: 68);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "AssignmentProfileGroups",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignmentProfileGroups",
                table: "AssignmentProfileGroups",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentProfileGroups_AssignmentProfileId_AzureGroupId_AssignmentTypeId_GroupModeId",
                table: "AssignmentProfileGroups",
                columns: new[] { "AssignmentProfileId", "AzureGroupId", "AssignmentTypeId", "GroupModeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignmentProfileGroups",
                table: "AssignmentProfileGroups");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentProfileGroups_AssignmentProfileId_AzureGroupId_AssignmentTypeId_GroupModeId",
                table: "AssignmentProfileGroups");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AssignmentProfileGroups");

            migrationBuilder.AlterColumn<Guid>(
                name: "AzureGroupId",
                table: "AssignmentProfileGroups",
                type: "uniqueidentifier",
                maxLength: 68,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldMaxLength: 68,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignmentProfileGroups",
                table: "AssignmentProfileGroups",
                columns: new[] { "AssignmentProfileId", "AzureGroupId", "AssignmentTypeId" });
        }
    }
}
