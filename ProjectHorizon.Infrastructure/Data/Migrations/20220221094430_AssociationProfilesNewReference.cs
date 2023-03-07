using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class AssociationProfilesNewReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentProfileGroups_AssignmentProfiles_AssignmentProfileId",
                table: "AssignmentProfileGroups",
                column: "AssignmentProfileId",
                principalTable: "AssignmentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentProfileGroups_AssignmentProfiles_AssignmentProfileId",
                table: "AssignmentProfileGroups");
        }
    }
}
