using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Certera.Data.Migrations
{
    public partial class DataModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainCertificates",
                columns: table => new
                {
                    DomainCertificateId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RawData = table.Column<string>(nullable: true),
                    Thumbprint = table.Column<string>(nullable: true),
                    SerialNumber = table.Column<string>(nullable: true),
                    ValidNotBefore = table.Column<DateTime>(nullable: false),
                    ValidNotAfter = table.Column<DateTime>(nullable: false),
                    Subject = table.Column<string>(nullable: true),
                    RegistrableDomain = table.Column<string>(nullable: true),
                    IssuerName = table.Column<string>(nullable: true),
                    CertificateSource = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainCertificates", x => x.DomainCertificateId);
                });

            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    DomainId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Uri = table.Column<string>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateLastScanned = table.Column<DateTime>(nullable: true),
                    RegistrableDomain = table.Column<string>(nullable: true),
                    Order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.DomainId);
                });

            migrationBuilder.CreateTable(
                name: "Keys",
                columns: table => new
                {
                    KeyId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    RawData = table.Column<string>(nullable: false),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModified = table.Column<DateTime>(nullable: false),
                    DateRotationReminder = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.KeyId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    NotificationSettingId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpirationAlerts = table.Column<bool>(nullable: false),
                    ChangeAlerts = table.Column<bool>(nullable: false),
                    AcquisitionFailureAlerts = table.Column<bool>(nullable: false),
                    ExpirationAlert1Day = table.Column<bool>(nullable: false),
                    ExpirationAlert3Days = table.Column<bool>(nullable: false),
                    ExpirationAlert7Days = table.Column<bool>(nullable: false),
                    ExpirationAlert14Days = table.Column<bool>(nullable: false),
                    ExpirationAlert30Days = table.Column<bool>(nullable: false),
                    AdditionalRecipients = table.Column<string>(nullable: true),
                    ApplicationUserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.NotificationSettingId);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    UserConfigurationId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    ApplicationUserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.UserConfigurationId);
                    table.ForeignKey(
                        name: "FK_UserConfigurations_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    UserNotificationId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ApplicationUserId = table.Column<long>(nullable: false),
                    DomainCertificateId = table.Column<long>(nullable: false),
                    NotificationEvent = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.UserNotificationId);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotifications_DomainCertificates_DomainCertificateId",
                        column: x => x.DomainCertificateId,
                        principalTable: "DomainCertificates",
                        principalColumn: "DomainCertificateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DomainCertificateChangeEvents",
                columns: table => new
                {
                    DomainCertificateChangeEventId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NewDomainCertificateId = table.Column<long>(nullable: false),
                    PreviousDomainCertificateId = table.Column<long>(nullable: false),
                    DomainId = table.Column<long>(nullable: false),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateProcessed = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainCertificateChangeEvents", x => x.DomainCertificateChangeEventId);
                    table.ForeignKey(
                        name: "FK_DomainCertificateChangeEvents_Domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "Domains",
                        principalColumn: "DomainId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DomainCertificateChangeEvents_DomainCertificates_NewDomainCertificateId",
                        column: x => x.NewDomainCertificateId,
                        principalTable: "DomainCertificates",
                        principalColumn: "DomainCertificateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DomainCertificateChangeEvents_DomainCertificates_PreviousDomainCertificateId",
                        column: x => x.PreviousDomainCertificateId,
                        principalTable: "DomainCertificates",
                        principalColumn: "DomainCertificateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcmeAccounts",
                columns: table => new
                {
                    AcmeAccountId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AcmeContactEmail = table.Column<string>(nullable: false),
                    AcmeAcceptTos = table.Column<bool>(nullable: false),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModified = table.Column<DateTime>(nullable: false),
                    IsAcmeStaging = table.Column<bool>(nullable: false),
                    KeyId = table.Column<long>(nullable: false),
                    ApplicationUserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeAccounts", x => x.AcmeAccountId);
                    table.ForeignKey(
                        name: "FK_AcmeAccounts_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcmeAccounts_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyHistories",
                columns: table => new
                {
                    KeyHistoryId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KeyId = table.Column<long>(nullable: false),
                    ApplicationUserId = table.Column<long>(nullable: true),
                    Operation = table.Column<string>(nullable: true),
                    DateOperation = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyHistories", x => x.KeyHistoryId);
                    table.ForeignKey(
                        name: "FK_KeyHistories_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyHistories_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DomainScans",
                columns: table => new
                {
                    DomainScanId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateScan = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ScanSuccess = table.Column<bool>(nullable: false),
                    ScanResult = table.Column<string>(nullable: true),
                    ScanStatus = table.Column<string>(nullable: true),
                    DomainId = table.Column<long>(nullable: false),
                    DomainCertificateId = table.Column<long>(nullable: true),
                    DomainCertificateChangeEventId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainScans", x => x.DomainScanId);
                    table.ForeignKey(
                        name: "FK_DomainScans_DomainCertificateChangeEvents_DomainCertificateChangeEventId",
                        column: x => x.DomainCertificateChangeEventId,
                        principalTable: "DomainCertificateChangeEvents",
                        principalColumn: "DomainCertificateChangeEventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DomainScans_DomainCertificates_DomainCertificateId",
                        column: x => x.DomainCertificateId,
                        principalTable: "DomainCertificates",
                        principalColumn: "DomainCertificateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DomainScans_Domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "Domains",
                        principalColumn: "DomainId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcmeCertificates",
                columns: table => new
                {
                    AcmeCertificateId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModified = table.Column<DateTime>(nullable: false),
                    Subject = table.Column<string>(nullable: false),
                    SANs = table.Column<string>(nullable: true),
                    KeyId = table.Column<long>(nullable: false),
                    ChallengeType = table.Column<string>(nullable: true),
                    CsrCountryName = table.Column<string>(nullable: true),
                    CsrState = table.Column<string>(nullable: true),
                    CsrLocality = table.Column<string>(nullable: true),
                    CsrOrganization = table.Column<string>(nullable: true),
                    CsrOrganizationUnit = table.Column<string>(nullable: true),
                    CsrCommonName = table.Column<string>(nullable: true),
                    AcmeAccountId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeCertificates", x => x.AcmeCertificateId);
                    table.ForeignKey(
                        name: "FK_AcmeCertificates_AcmeAccounts_AcmeAccountId",
                        column: x => x.AcmeAccountId,
                        principalTable: "AcmeAccounts",
                        principalColumn: "AcmeAccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcmeCertificates_Keys_KeyId",
                        column: x => x.KeyId,
                        principalTable: "Keys",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcmeOrders",
                columns: table => new
                {
                    AcmeOrderId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RequestCount = table.Column<int>(nullable: false),
                    InvalidResponseCount = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Errors = table.Column<string>(nullable: true),
                    RawDataPem = table.Column<string>(nullable: true),
                    AcmeCertificateId = table.Column<long>(nullable: false),
                    DomainCertificateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeOrders", x => x.AcmeOrderId);
                    table.ForeignKey(
                        name: "FK_AcmeOrders_AcmeCertificates_AcmeCertificateId",
                        column: x => x.AcmeCertificateId,
                        principalTable: "AcmeCertificates",
                        principalColumn: "AcmeCertificateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcmeOrders_DomainCertificates_DomainCertificateId",
                        column: x => x.DomainCertificateId,
                        principalTable: "DomainCertificates",
                        principalColumn: "DomainCertificateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcmeRequests",
                columns: table => new
                {
                    AcmeRequestId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Token = table.Column<string>(nullable: true),
                    KeyAuthorization = table.Column<string>(nullable: true),
                    AcmeOrderId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcmeRequests", x => x.AcmeRequestId);
                    table.ForeignKey(
                        name: "FK_AcmeRequests_AcmeOrders_AcmeOrderId",
                        column: x => x.AcmeOrderId,
                        principalTable: "AcmeOrders",
                        principalColumn: "AcmeOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcmeAccounts_ApplicationUserId",
                table: "AcmeAccounts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeAccounts_KeyId",
                table: "AcmeAccounts",
                column: "KeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_AcmeAccountId",
                table: "AcmeCertificates",
                column: "AcmeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_KeyId",
                table: "AcmeCertificates",
                column: "KeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_Name_AcmeAccountId",
                table: "AcmeCertificates",
                columns: new[] { "Name", "AcmeAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcmeCertificates_Subject_AcmeAccountId",
                table: "AcmeCertificates",
                columns: new[] { "Subject", "AcmeAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcmeOrders_AcmeCertificateId",
                table: "AcmeOrders",
                column: "AcmeCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeOrders_DomainCertificateId",
                table: "AcmeOrders",
                column: "DomainCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeRequests_AcmeOrderId",
                table: "AcmeRequests",
                column: "AcmeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AcmeRequests_Token",
                table: "AcmeRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainCertificateChangeEvents_DomainId",
                table: "DomainCertificateChangeEvents",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainCertificateChangeEvents_NewDomainCertificateId",
                table: "DomainCertificateChangeEvents",
                column: "NewDomainCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainCertificateChangeEvents_PreviousDomainCertificateId",
                table: "DomainCertificateChangeEvents",
                column: "PreviousDomainCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_DateLastScanned",
                table: "Domains",
                column: "DateLastScanned");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Uri",
                table: "Domains",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainScans_DomainCertificateChangeEventId",
                table: "DomainScans",
                column: "DomainCertificateChangeEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainScans_DomainCertificateId",
                table: "DomainScans",
                column: "DomainCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainScans_DomainId",
                table: "DomainScans",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyHistories_ApplicationUserId",
                table: "KeyHistories",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyHistories_KeyId",
                table: "KeyHistories",
                column: "KeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Keys_Name",
                table: "Keys",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_ApplicationUserId",
                table: "NotificationSettings",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConfigurations_ApplicationUserId",
                table: "UserConfigurations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ApplicationUserId",
                table: "UserNotifications",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_DomainCertificateId",
                table: "UserNotifications",
                column: "DomainCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_NotificationEvent",
                table: "UserNotifications",
                column: "NotificationEvent");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcmeRequests");

            migrationBuilder.DropTable(
                name: "DomainScans");

            migrationBuilder.DropTable(
                name: "KeyHistories");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "UserConfigurations");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "AcmeOrders");

            migrationBuilder.DropTable(
                name: "DomainCertificateChangeEvents");

            migrationBuilder.DropTable(
                name: "AcmeCertificates");

            migrationBuilder.DropTable(
                name: "Domains");

            migrationBuilder.DropTable(
                name: "DomainCertificates");

            migrationBuilder.DropTable(
                name: "AcmeAccounts");

            migrationBuilder.DropTable(
                name: "Keys");
        }
    }
}
