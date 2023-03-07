using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class FarPayTokenAndDigits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditCardDigits",
                table: "Subscriptions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarPayToken",
                table: "Subscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditCardDigits",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "FarPayToken",
                table: "Subscriptions");
        }
    }
}
