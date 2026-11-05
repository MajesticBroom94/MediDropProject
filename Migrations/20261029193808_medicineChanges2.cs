using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class medicineChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipType",
                table: "PatientCareRelationships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationshipType",
                table: "PatientCareRelationships",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
