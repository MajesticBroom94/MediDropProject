using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class removeUnusedDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_CorrelationId",
                table: "ApiRequestLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_DeviceId_StartedAt",
                table: "ApiRequestLogs",
                columns: new[] { "DeviceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestId",
                table: "ApiRequestLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_StartedAt",
                table: "ApiRequestLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_UserId_StartedAt",
                table: "ApiRequestLogs",
                columns: new[] { "UserId", "StartedAt" });
        }
    }
}
