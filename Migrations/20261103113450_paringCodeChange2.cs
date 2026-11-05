using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class paringCodeChange2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "DevicePairingCodes");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes",
                column: "DeviceId",
                unique: true,
                filter: "[InvalidatedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UsedAt",
                table: "DevicePairingCodes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePairingCodes_DeviceId",
                table: "DevicePairingCodes",
                column: "DeviceId");
        }
    }
}
