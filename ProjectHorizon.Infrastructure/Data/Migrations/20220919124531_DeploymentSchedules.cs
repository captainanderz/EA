using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class DeploymentSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DeploymentScheduleId",
                table: "SubscriptionPublicApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeploymentScheduleId",
                table: "PrivateApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AssignmentProfiles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "DeploymentSchedulePhases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OffsetDays = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    DeploymentScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentProfileId = table.Column<long>(type: "bigint", nullable: false),
                    UseRequirementScript = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSchedulePhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentSchedulePhases_AssignmentProfiles_AssignmentProfileId",
                        column: x => x.AssignmentProfileId,
                        principalTable: "AssignmentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CronTrigger = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentPhaseId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentSchedules_DeploymentSchedulePhases_CurrentPhaseId",
                        column: x => x.CurrentPhaseId,
                        principalTable: "DeploymentSchedulePhases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentSchedules_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentSchedulePrivateApplications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeploymentScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    IntuneId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSchedulePrivateApplications", x => new { x.ApplicationId, x.Type });
                    table.ForeignKey(
                        name: "FK_DeploymentSchedulePrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                        column: x => x.DeploymentScheduleId,
                        principalTable: "DeploymentSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentSchedulePrivateApplications_PrivateApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PrivateApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeploymentScheduleSubscriptionPublicApplications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeploymentScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    IntuneId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentScheduleSubscriptionPublicApplications", x => new { x.SubscriptionId, x.ApplicationId, x.Type });
                    table.ForeignKey(
                        name: "FK_DeploymentScheduleSubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                        column: x => x.DeploymentScheduleId,
                        principalTable: "DeploymentSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentScheduleSubscriptionPublicApplications_SubscriptionPublicApplications_SubscriptionId_ApplicationId",
                        columns: x => new { x.SubscriptionId, x.ApplicationId },
                        principalTable: "SubscriptionPublicApplications",
                        principalColumns: new[] { "SubscriptionId", "PublicApplicationId" });
                    table.ForeignKey(
                        name: "FK_DeploymentScheduleSubscriptionPublicApplications_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPublicApplications_DeploymentScheduleId",
                table: "SubscriptionPublicApplications",
                column: "DeploymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateApplications_DeploymentScheduleId",
                table: "PrivateApplications",
                column: "DeploymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedulePhases_AssignmentProfileId",
                table: "DeploymentSchedulePhases",
                column: "AssignmentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedulePhases_DeploymentScheduleId",
                table: "DeploymentSchedulePhases",
                column: "DeploymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedulePrivateApplications_DeploymentScheduleId",
                table: "DeploymentSchedulePrivateApplications",
                column: "DeploymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedules_CurrentPhaseId",
                table: "DeploymentSchedules",
                column: "CurrentPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSchedules_SubscriptionId",
                table: "DeploymentSchedules",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentScheduleSubscriptionPublicApplications_DeploymentScheduleId",
                table: "DeploymentScheduleSubscriptionPublicApplications",
                column: "DeploymentScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "PrivateApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "SubscriptionPublicApplications",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases",
                column: "DeploymentScheduleId",
                principalTable: "DeploymentSchedules",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "PrivateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPublicApplications_DeploymentSchedules_DeploymentScheduleId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentSchedulePhases_DeploymentSchedules_DeploymentScheduleId",
                table: "DeploymentSchedulePhases");

            migrationBuilder.DropTable(
                name: "DeploymentSchedulePrivateApplications");

            migrationBuilder.DropTable(
                name: "DeploymentScheduleSubscriptionPublicApplications");

            migrationBuilder.DropTable(
                name: "DeploymentSchedules");

            migrationBuilder.DropTable(
                name: "DeploymentSchedulePhases");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPublicApplications_DeploymentScheduleId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropIndex(
                name: "IX_PrivateApplications_DeploymentScheduleId",
                table: "PrivateApplications");

            migrationBuilder.DropColumn(
                name: "DeploymentScheduleId",
                table: "SubscriptionPublicApplications");

            migrationBuilder.DropColumn(
                name: "DeploymentScheduleId",
                table: "PrivateApplications");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AssignmentProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);
        }
    }
}
