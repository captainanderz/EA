using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class InvitationTokenMaxLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "UserInvitations");

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "UserInvitations",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "UserInvitations");

            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "UserInvitations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
