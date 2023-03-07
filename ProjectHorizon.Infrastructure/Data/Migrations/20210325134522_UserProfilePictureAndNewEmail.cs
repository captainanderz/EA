using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class UserProfilePictureAndNewEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewUnconfirmedEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureOriginal",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureSmall",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewUnconfirmedEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureOriginal",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureSmall",
                table: "AspNetUsers");
        }
    }
}
