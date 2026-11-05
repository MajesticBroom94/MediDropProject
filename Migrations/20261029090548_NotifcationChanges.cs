using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class NotifcationChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_DeliveryStatus_NextDeliveryAttemptAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryAttempts",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "NextDeliveryAttemptAt",
                table: "Notifications",
                newName: "RemovedAt");

            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "RemovedAt",
                table: "Notifications",
                newName: "NextDeliveryAttemptAt");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempts",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeliveryStatus_NextDeliveryAttemptAt",
                table: "Notifications",
                columns: new[] { "DeliveryStatus", "NextDeliveryAttemptAt" });
        }
    }
}
