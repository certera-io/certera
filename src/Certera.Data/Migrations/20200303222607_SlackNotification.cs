using Microsoft.EntityFrameworkCore.Migrations;

namespace Certera.Data.Migrations
{
    public partial class SlackNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SendEmailNotification",
                table: "NotificationSettings",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendSlackNotification",
                table: "NotificationSettings",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SlackWebhookUrl",
                table: "NotificationSettings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
