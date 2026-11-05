using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class modifyProviderReg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "CareProviderRegistrations",
                newName: "StatusReason");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastStatusChangedAt",
                table: "CareProviderRegistrations",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastStatusChangedAt",
                table: "CareProviderRegistrations");

            migrationBuilder.RenameColumn(
                name: "StatusReason",
                table: "CareProviderRegistrations",
                newName: "RejectionReason");
        }
    }
}
