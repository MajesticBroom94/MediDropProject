using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class modifyPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Patients");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Patients",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Patients");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Patients",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
