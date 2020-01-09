using Microsoft.EntityFrameworkCore.Migrations;

namespace Certera.Data.Migrations
{
    public partial class IndividualApiKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey1",
                table: "Keys",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKey2",
                table: "Keys",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKey1",
                table: "AcmeCertificates",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKey2",
                table: "AcmeCertificates",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Keys_ApiKey1",
                table: "Keys",
                column: "ApiKey1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Keys_ApiKey2",
                table: "Keys",
                column: "ApiKey2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_ApiKey1",
                table: "AcmeCertificates",
                column: "ApiKey1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_ApiKey2",
                table: "AcmeCertificates",
                column: "ApiKey2",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Keys_ApiKey1",
                table: "Keys");

            migrationBuilder.DropIndex(
                name: "IX_Keys_ApiKey2",
                table: "Keys");

            migrationBuilder.DropIndex(
                name: "IX_AcmeCertificates_ApiKey1",
                table: "AcmeCertificates");

            migrationBuilder.DropIndex(
                name: "IX_AcmeCertificates_ApiKey2",
                table: "AcmeCertificates");
        }
    }
}
