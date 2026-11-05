using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class addLastRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastSelectedProfile",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSelectedProfile",
                table: "AspNetUsers");
        }
    }
}
