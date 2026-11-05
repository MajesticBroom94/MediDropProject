using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class modifyProfileDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationReviews");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "DeviceCommands");

            migrationBuilder.RenameColumn(
                name: "LastStatusChangedAt",
                table: "Patients",
                newName: "LastStatusChange");

            migrationBuilder.RenameColumn(
                name: "LastStatusChangedAt",
                table: "CareProviderRegistrations",
                newName: "LastStatusChange");

            migrationBuilder.AddColumn<int>(
                name: "StatusChangedByUserId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusChangedByUserId",
                table: "CareProviderRegistrations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusChangedByUserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "StatusChangedByUserId",
                table: "CareProviderRegistrations");

            migrationBuilder.RenameColumn(
                name: "LastStatusChange",
                table: "Patients",
                newName: "LastStatusChangedAt");

            migrationBuilder.RenameColumn(
                name: "LastStatusChange",
                table: "CareProviderRegistrations",
                newName: "LastStatusChangedAt");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "DeviceCommands",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicationReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationReviews", x => x.Id);
                    table.CheckConstraint("CK_MedicationReview_Rating", "[Rating] >= 1 AND [Rating] <= 5");
                    table.ForeignKey(
                        name: "FK_MedicationReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationReviews_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReviews_MedicationId_UserId",
                table: "MedicationReviews",
                columns: new[] { "MedicationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReviews_UserId",
                table: "MedicationReviews",
                column: "UserId");
        }
    }
}
