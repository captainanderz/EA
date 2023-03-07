using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class InitAssociationProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AssignmentProfileId",
                table: "SubscriptionPublicApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignmentProfileId",
                table: "PrivateApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureSmall",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewUnconfirmedEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentProfileGroups",
                columns: table => new
                {
                    AssignmentProfileId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentTypeId = table.Column<int>(type: "int", nullable: false),
                    AzureGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilterId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 68, nullable: false),
                    FilterModeId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentProfileGroups", x => new { x.AssignmentProfileId, x.AzureGroupId, x.AssignmentTypeId });
                },
                comment: "A collection of groups defined by IT-administrators to deploy one or more applications to");

            migrationBuilder.CreateTable(
                name: "AssignmentProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Keys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Use = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Algorithm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsX509Certificate = table.Column<bool>(type: "bit", nullable: false),
                    DataProtected = table.Column<bool>(type: "bit", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPublicApplications_AssignmentProfileId",
                table: "SubscriptionPublicApplications",
                column: "AssignmentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateApplications_AssignmentProfileId",
                table: "PrivateApplications",
                column: "AssignmentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistedGrants_ConsumedTime",
                table: "PersistedGrants",
                column: "ConsumedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Keys_Use",
                table: "Keys",
                column: "Use");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateApplications_AssignmentProfiles_AssignmentProfileId",
                table: "PrivateApplications",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPublicApplications_AssignmentProfiles_AssignmentProfileId",
                table: "SubscriptionPublicApplications",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateApplications_AssignmentProfiles_AssignmentProfileId",
                table: "PrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPublicApplications_AssignmentProfiles_AssignmentProfileId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropTable(
                name: "AssignmentProfileGroups");

            migrationBuilder.DropTable(
                name: "AssignmentProfiles");

            migrationBuilder.DropTable(
                name: "Keys");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPublicApplications_AssignmentProfileId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_PrivateApplications_AssignmentProfileId",
                table: "PrivateApplications");

            migrationBuilder.DropIndex(
                name: "IX_PersistedGrants_ConsumedTime",
                table: "PersistedGrants");

            migrationBuilder.DropColumn(
                name: "AssignmentProfileId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "AssignmentProfileId",
                table: "PrivateApplications");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureSmall",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NewUnconfirmedEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
