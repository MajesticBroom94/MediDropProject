using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class settingsChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DispenserDevices_PairingCodeHash",
                table: "DispenserDevices");

            migrationBuilder.DropColumn(
                name: "CareProviderAlertsEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "DeviceAlertsEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "MedicationRemindersEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PairingCodeHash",
                table: "DispenserDevices");

            migrationBuilder.AddColumn<int>(
                name: "ReminderLeadMinutes",
                table: "UserSettings",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.CreateTable(
                name: "DevicePairingCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ProtectedCode = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePairingCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevicePairingCodes_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserSetting_ReminderLeadMinutes",
                table: "UserSettings",
                sql: "[ReminderLeadMinutes] >= 5 AND [ReminderLeadMinutes] <= 120");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_CodeHash",
                table: "DevicePairingCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes",
                column: "DeviceId",
                unique: true,
                filter: "[UsedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevicePairingCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserSetting_ReminderLeadMinutes",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ReminderLeadMinutes",
                table: "UserSettings");

            migrationBuilder.AddColumn<bool>(
                name: "CareProviderAlertsEnabled",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeviceAlertsEnabled",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MedicationRemindersEnabled",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PairingCodeHash",
                table: "DispenserDevices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispenserDevices_PairingCodeHash",
                table: "DispenserDevices",
                column: "PairingCodeHash",
                unique: true,
                filter: "[PairingCodeHash] IS NOT NULL");
        }
    }
}
