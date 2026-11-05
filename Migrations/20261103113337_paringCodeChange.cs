using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class paringCodeChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvalidatedAt",
                table: "DevicePairingCodes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes");

            migrationBuilder.DropColumn(
                name: "InvalidatedAt",
                table: "DevicePairingCodes");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes",
                column: "DeviceId",
                unique: true,
                filter: "[UsedAt] IS NULL");
        }
    }
}
