using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class medicineChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanManageDevice",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanModifyMedications",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanModifySchedules",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanViewAlerts",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanViewDispensing",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanViewMedications",
                table: "PatientCareRelationships");

            migrationBuilder.DropColumn(
                name: "CanViewPatient",
                table: "PatientCareRelationships");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Medications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "Medications",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ApprovalStatus", "SubmittedByUserId" },
                values: new object[] { "Approved", null });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_ApprovalStatus",
                table: "Medications",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_SubmittedByUserId",
                table: "Medications",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_AspNetUsers_SubmittedByUserId",
                table: "Medications",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medications_AspNetUsers_SubmittedByUserId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_ApprovalStatus",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_SubmittedByUserId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Medications");

            migrationBuilder.AddColumn<bool>(
                name: "CanManageDevice",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanModifyMedications",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanModifySchedules",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewAlerts",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewDispensing",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewMedications",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewPatient",
                table: "PatientCareRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
