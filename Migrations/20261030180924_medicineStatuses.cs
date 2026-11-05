using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class medicineStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Medications");

            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "Medications",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "IX_Medications_ApprovalStatus",
                table: "Medications",
                newName: "IX_Medications_Status");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastStatusChangedAt",
                table: "Patients",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "Patients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 1,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 2,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 3,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 4,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 5,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 6,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 7,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 8,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 9,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 10,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 11,
                column: "Status",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 12,
                column: "Status",
                value: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials",
                column: "DeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials");

            migrationBuilder.DropColumn(
                name: "LastStatusChangedAt",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Medications",
                newName: "ApprovalStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Medications_Status",
                table: "Medications",
                newName: "IX_Medications_ApprovalStatus");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ApprovalStatus", "IsActive" },
                values: new object[] { "Approved", true });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials",
                column: "DeviceId");
        }
    }
}
