using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_DispenserDevices_DeviceId",
                table: "AuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_Patients_PatientId",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_CorrelationId",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_DeviceId_OccurredAt",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_EventId",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_PatientId_OccurredAt",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_RequestId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditEvents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeviceId",
                table: "AuditEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditEvents",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "AuditEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecordedAt",
                table: "AuditEvents",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CorrelationId",
                table: "AuditEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_DeviceId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "DeviceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventId",
                table: "AuditEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PatientId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "PatientId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_RequestId",
                table: "AuditEvents",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_DispenserDevices_DeviceId",
                table: "AuditEvents",
                column: "DeviceId",
                principalTable: "DispenserDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_Patients_PatientId",
                table: "AuditEvents",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
