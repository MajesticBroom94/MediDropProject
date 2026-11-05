using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MediDropProject.Migrations
{
    /// <inheritdoc />
    public partial class createNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispenserDevices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceUid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PairingCodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OperatingState = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenserDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationManufacturers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationManufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProfessionalTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareProviders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FirebaseDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    InstallationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FcmToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirebaseDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirebaseDeviceTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryAttempts = table.Column<int>(type: "int", nullable: false),
                    NextDeliveryAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveryError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MorningDispenseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MiddayDispenseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EveningDispenseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MedicationRemindersEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DeviceAlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CareProviderAlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "DeviceCommands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCommands_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeviceCommands_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    CredentialHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCredentials_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSlots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    SlotNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSlots", x => x.Id);
                    table.UniqueConstraint("AK_DeviceSlots_DeviceId_Id", x => new { x.DeviceId, x.Id });
                    table.CheckConstraint("CK_DeviceSlot_SlotNumber", "[SlotNumber] > 0");
                    table.ForeignKey(
                        name: "FK_DeviceSlots_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Form = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManufacturerId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ActiveIngredients = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SafetyInformation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medications_MedicationManufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "MedicationManufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditEvents_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeviceAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceAssignments_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceAssignments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationRounds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationRounds_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationRounds_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientCareRelationships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    CareProviderId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GrantedByUserId = table.Column<int>(type: "int", nullable: true),
                    CanViewPatient = table.Column<bool>(type: "bit", nullable: false),
                    CanViewMedications = table.Column<bool>(type: "bit", nullable: false),
                    CanViewDispensing = table.Column<bool>(type: "bit", nullable: false),
                    CanViewAlerts = table.Column<bool>(type: "bit", nullable: false),
                    CanModifySchedules = table.Column<bool>(type: "bit", nullable: false),
                    CanModifyMedications = table.Column<bool>(type: "bit", nullable: false),
                    CanManageDevice = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCareRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCareRelationships_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientCareRelationships_CareProviders_CareProviderId",
                        column: x => x.CareProviderId,
                        principalTable: "CareProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientCareRelationships_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstDispensedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastDispensedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => x.Id);
                    table.CheckConstraint("CK_PatientMedication_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareProviderRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareProviderId = table.Column<int>(type: "int", nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareProviderRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareProviderRegistrations_CareProviders_CareProviderId",
                        column: x => x.CareProviderId,
                        principalTable: "CareProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareProviderRegistrations_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PrescribedById = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RepeatCount = table.Column<int>(type: "int", nullable: false),
                    CollectedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_CareProviders_PrescribedById",
                        column: x => x.PrescribedById,
                        principalTable: "CareProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceScheduleSyncs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    MedicationRoundId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduleVersion = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceScheduleSyncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceScheduleSyncs_DispenserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DispenserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceScheduleSyncs_MedicationRounds_MedicationRoundId",
                        column: x => x.MedicationRoundId,
                        principalTable: "MedicationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationRoundSlots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationRoundId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceSlotId = table.Column<long>(type: "bigint", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DispensedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastNotifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationRoundSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationRoundSlots_DeviceSlots_DeviceSlotId",
                        column: x => x.DeviceSlotId,
                        principalTable: "DeviceSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationRoundSlots_MedicationRounds_MedicationRoundId",
                        column: x => x.MedicationRoundId,
                        principalTable: "MedicationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    FillNumber = table.Column<int>(type: "int", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionCollections_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    PatientMedicationId = table.Column<long>(type: "bigint", nullable: true),
                    DosageAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DosageUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QuantityPrescribed = table.Column<int>(type: "int", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_PatientMedications_PatientMedicationId",
                        column: x => x.PatientMedicationId,
                        principalTable: "PatientMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationRoundSlotItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationRoundSlotId = table.Column<long>(type: "bigint", nullable: false),
                    PatientMedicationId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MedicationStrength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MedicationForm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationRoundSlotItems", x => x.Id);
                    table.CheckConstraint("CK_MedicationRoundSlotItem_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_MedicationRoundSlotItems_MedicationRoundSlots_MedicationRoundSlotId",
                        column: x => x.MedicationRoundSlotId,
                        principalTable: "MedicationRoundSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationRoundSlotItems_PatientMedications_PatientMedicationId",
                        column: x => x.PatientMedicationId,
                        principalTable: "PatientMedications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Medications",
                columns: new[] { "Id", "ActiveIngredients", "Barcode", "CreatedAt", "Description", "Form", "GenericName", "ImageUrl", "IsActive", "ManufacturerId", "Name", "SafetyInformation", "Strength", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Analgesic and antipyretic used for mild to moderate pain and fever.", "Tablet", "Paracetamol", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Paracetamol", null, "500 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "NSAID used to relieve pain, inflammation and fever.", "Tablet", "Ibuprofen", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Ibuprofen", null, "200 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Penicillin antibiotic used for susceptible bacterial infections.", "Capsule", "Amoxicillin", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Amoxicillin", null, "500 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medicine used to control blood glucose in type 2 diabetes.", "Tablet", "Metformin hydrochloride", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Metformin", null, "500 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Calcium channel blocker used for hypertension and angina.", "Tablet", "Amlodipine", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Amlodipine", null, "5 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Statin used to lower cholesterol and cardiovascular risk.", "Tablet", "Atorvastatin", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Atorvastatin", null, "20 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Angiotensin II receptor blocker used for hypertension.", "Tablet", "Losartan potassium", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Losartan", null, "50 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Proton pump inhibitor used to reduce stomach acid.", "Gastro-resistant capsule", "Omeprazole", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Omeprazole", null, "20 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 9, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Antihistamine used for allergic rhinitis and urticaria.", "Tablet", "Cetirizine hydrochloride", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Cetirizine", null, "10 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 10, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Antihistamine used to relieve allergy symptoms.", "Tablet", "Loratadine", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Loratadine", null, "10 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 11, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Calcium channel blocker used for hypertension and angina.", "Tablet", "Amlodipine", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Amlodipine", null, "10 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 12, null, null, new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Biguanide used to improve glucose control in type 2 diabetes.", "Tablet", "Metformin hydrochloride", "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg", true, null, "Metformin", null, "850 mg", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
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

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorUserId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "ActorUserId", "OccurredAt" });

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
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PatientId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "PatientId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_RequestId",
                table: "AuditEvents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CareProviderRegistrations_CareProviderId_SubmittedAt",
                table: "CareProviderRegistrations",
                columns: new[] { "CareProviderId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareProviderRegistrations_DocumentId",
                table: "CareProviderRegistrations",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CareProviders_UserId",
                table: "CareProviders",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAssignments_AssignedAt",
                table: "DeviceAssignments",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAssignments_DeviceId",
                table: "DeviceAssignments",
                column: "DeviceId",
                unique: true,
                filter: "[UnassignedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAssignments_DeviceId_Status",
                table: "DeviceAssignments",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAssignments_PatientId_Status",
                table: "DeviceAssignments",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_DeviceId_CreatedAt",
                table: "DeviceCommands",
                columns: new[] { "DeviceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_DeviceId_Status",
                table: "DeviceCommands",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_OperationId",
                table: "DeviceCommands",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_RequestedByUserId",
                table: "DeviceCommands",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_CredentialHash",
                table: "DeviceCredentials",
                column: "CredentialHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_EventId",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_OperationId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "OperationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_EventType_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceScheduleSyncs_DeviceId_CreatedAt",
                table: "DeviceScheduleSyncs",
                columns: new[] { "DeviceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceScheduleSyncs_DeviceId_MedicationRoundId_ScheduleVersion",
                table: "DeviceScheduleSyncs",
                columns: new[] { "DeviceId", "MedicationRoundId", "ScheduleVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceScheduleSyncs_MedicationRoundId",
                table: "DeviceScheduleSyncs",
                column: "MedicationRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSlots_DeviceId_SlotNumber",
                table: "DeviceSlots",
                columns: new[] { "DeviceId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispenserDevices_DeviceUid",
                table: "DispenserDevices",
                column: "DeviceUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispenserDevices_PairingCodeHash",
                table: "DispenserDevices",
                column: "PairingCodeHash",
                unique: true,
                filter: "[PairingCodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PatientId",
                table: "Documents",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StorageKey",
                table: "Documents",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_InstallationId",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "InstallationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_LastUsedAt",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "LastUsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseDeviceTokens_UserId_RevokedAt",
                table: "FirebaseDeviceTokens",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationManufacturers_Name",
                table: "MedicationManufacturers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReviews_MedicationId_UserId",
                table: "MedicationReviews",
                columns: new[] { "MedicationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReviews_UserId",
                table: "MedicationReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRounds_DeviceId",
                table: "MedicationRounds",
                column: "DeviceId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRounds_DeviceId_CreatedAt",
                table: "MedicationRounds",
                columns: new[] { "DeviceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRounds_PatientId_CreatedAt",
                table: "MedicationRounds",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRounds_Status_CreatedAt",
                table: "MedicationRounds",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlotItems_MedicationRoundSlotId_PatientMedicationId",
                table: "MedicationRoundSlotItems",
                columns: new[] { "MedicationRoundSlotId", "PatientMedicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlotItems_PatientMedicationId",
                table: "MedicationRoundSlotItems",
                column: "PatientMedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlots_DeviceSlotId_ScheduledFor",
                table: "MedicationRoundSlots",
                columns: new[] { "DeviceSlotId", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlots_MedicationRoundId_ScheduledFor",
                table: "MedicationRoundSlots",
                columns: new[] { "MedicationRoundId", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlots_OperationId",
                table: "MedicationRoundSlots",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoundSlots_Status_ScheduledFor",
                table: "MedicationRoundSlots",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Barcode",
                table: "Medications",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_ManufacturerId",
                table: "Medications",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name_Strength_Form_ManufacturerId",
                table: "Medications",
                columns: new[] { "Name", "Strength", "Form", "ManufacturerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeliveryStatus_NextDeliveryAttemptAt",
                table: "Notifications",
                columns: new[] { "DeliveryStatus", "NextDeliveryAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_DeduplicationKey",
                table: "Notifications",
                columns: new[] { "UserId", "DeduplicationKey" },
                unique: true,
                filter: "[DeduplicationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ReadAt_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientCareRelationships_CareProviderId",
                table: "PatientCareRelationships",
                column: "CareProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCareRelationships_GrantedByUserId",
                table: "PatientCareRelationships",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCareRelationships_PatientId_CareProviderId_Status",
                table: "PatientCareRelationships",
                columns: new[] { "PatientId", "CareProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationId",
                table: "PatientMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId_MedicationId",
                table: "PatientMedications",
                columns: new[] { "PatientId", "MedicationId" },
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId_Status",
                table: "PatientMedications",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionCollections_PrescriptionId_FillNumber",
                table: "PrescriptionCollections",
                columns: new[] { "PrescriptionId", "FillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicationId",
                table: "PrescriptionItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PatientMedicationId_PrescriptionId",
                table: "PrescriptionItems",
                columns: new[] { "PatientMedicationId", "PrescriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId_MedicationId",
                table: "PrescriptionItems",
                columns: new[] { "PrescriptionId", "MedicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DocumentId",
                table: "Prescriptions",
                column: "DocumentId",
                unique: true,
                filter: "[DocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescribedById",
                table: "Prescriptions",
                column: "PrescribedById");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                column: "PrescriptionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OccurredAt",
                table: "SecurityEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Type_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "Type", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "CareProviderRegistrations");

            migrationBuilder.DropTable(
                name: "DeviceAssignments");

            migrationBuilder.DropTable(
                name: "DeviceCommands");

            migrationBuilder.DropTable(
                name: "DeviceCredentials");

            migrationBuilder.DropTable(
                name: "DeviceEvents");

            migrationBuilder.DropTable(
                name: "DeviceScheduleSyncs");

            migrationBuilder.DropTable(
                name: "FirebaseDeviceTokens");

            migrationBuilder.DropTable(
                name: "MedicationReviews");

            migrationBuilder.DropTable(
                name: "MedicationRoundSlotItems");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PatientCareRelationships");

            migrationBuilder.DropTable(
                name: "PrescriptionCollections");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "MedicationRoundSlots");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "DeviceSlots");

            migrationBuilder.DropTable(
                name: "MedicationRounds");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "CareProviders");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DispenserDevices");

            migrationBuilder.DropTable(
                name: "MedicationManufacturers");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
