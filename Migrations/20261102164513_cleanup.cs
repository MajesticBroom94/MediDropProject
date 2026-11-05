using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class cleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirebaseDeviceTokens");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "UserSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "UserSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FirebaseDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AppVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FcmToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirebaseDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirebaseDeviceTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_InstallationId",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "InstallationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_LastUsedAt",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "LastUsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_RevokedAt",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "RevokedAt" });
        }
    }
}
