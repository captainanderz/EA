using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectHorizon.Infrastructure.Data.Migrations
{
    public partial class SubscriptionDeviceCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceCount",
                table: "Subscriptions",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceCount",
                table: "Subscriptions");
        }
    }
}
