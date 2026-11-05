using MediDropProject.Domain.Entities;
using MediDropProject.Data.Configuration;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<CareProvider> CareProviders => Set<CareProvider>();
    public DbSet<CareProviderRegistration> CareProviderRegistrations => Set<CareProviderRegistration>();
    public DbSet<DeviceAssignment> DeviceAssignments => Set<DeviceAssignment>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
    public DbSet<DeviceSlot> DeviceSlots => Set<DeviceSlot>();
    public DbSet<DispenserDevice> DispenserDevices => Set<DispenserDevice>();
    public DbSet<DevicePairingCode> DevicePairingCodes => Set<DevicePairingCode>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationManufacturer> MedicationManufacturers => Set<MedicationManufacturer>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientCareRelationship> PatientCareRelationships => Set<PatientCareRelationship>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionCollection> PrescriptionCollections => Set<PrescriptionCollection>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<MedicationRound> MedicationRounds => Set<MedicationRound>();
    public DbSet<MedicationRoundSlot> MedicationRoundSlots => Set<MedicationRoundSlot>();
    public DbSet<MedicationRoundSlotItem> MedicationRoundSlotItems => Set<MedicationRoundSlotItem>();
    public DbSet<DeviceScheduleSync> DeviceScheduleSyncs => Set<DeviceScheduleSync>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationUserConfiguration).Assembly);


        var seedDate = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

        const string imageUrl = "https://medidropimages.blob.core.windows.net/medicines/medication_bottle_placeholder_image.jpg";

        var medications = new[]
        {
            new Medication
            {
                Id = 1,
                Name = "Paracetamol",
                GenericName = "Paracetamol",
                Strength = "500 mg",
                Form = "Tablet",
                Description = "Analgesic and antipyretic used for mild to moderate pain and fever."
            },
            new Medication
            {
                Id = 2,
                Name = "Ibuprofen",
                GenericName = "Ibuprofen",
                Strength = "200 mg",
                Form = "Tablet",
                Description = "NSAID used to relieve pain, inflammation and fever."
            },
            new Medication
            {
                Id = 3,
                Name = "Amoxicillin",
                GenericName = "Amoxicillin",
                Strength = "500 mg",
                Form = "Capsule",
                Description = "Penicillin antibiotic used for susceptible bacterial infections."
            },
            new Medication
            {
                Id = 4,
                Name = "Metformin",
                GenericName = "Metformin hydrochloride",
                Strength = "500 mg",
                Form = "Tablet",
                Description = "Medicine used to control blood glucose in type 2 diabetes."
            },
            new Medication
            {
                Id = 5,
                Name = "Amlodipine",
                GenericName = "Amlodipine",
                Strength = "5 mg",
                Form = "Tablet",
                Description = "Calcium channel blocker used for hypertension and angina."
            },
            new Medication
            {
                Id = 6,
                Name = "Atorvastatin",
                GenericName = "Atorvastatin",
                Strength = "20 mg",
                Form = "Tablet",
                Description = "Statin used to lower cholesterol and cardiovascular risk."
            },
            new Medication
            {
                Id = 7,
                Name = "Losartan",
                GenericName = "Losartan potassium",
                Strength = "50 mg",
                Form = "Tablet",
                Description = "Angiotensin II receptor blocker used for hypertension."
            },
            new Medication
            {
                Id = 8,
                Name = "Omeprazole",
                GenericName = "Omeprazole",
                Strength = "20 mg",
                Form = "Gastro-resistant capsule",
                Description = "Proton pump inhibitor used to reduce stomach acid."
            },
            new Medication
            {
                Id = 9,
                Name = "Cetirizine",
                GenericName = "Cetirizine hydrochloride",
                Strength = "10 mg",
                Form = "Tablet",
                Description = "Antihistamine used for allergic rhinitis and urticaria."
            },
            new Medication
            {
                Id = 10,
                Name = "Loratadine",
                GenericName = "Loratadine",
                Strength = "10 mg",
                Form = "Tablet",
                Description = "Antihistamine used to relieve allergy symptoms."
            },
            new Medication
            {
                Id = 11,
                Name = "Amlodipine",
                GenericName = "Amlodipine",
                Strength = "10 mg",
                Form = "Tablet",
                Description = "Calcium channel blocker used for hypertension and angina."
            },
            new Medication
            {
                Id = 12,
                Name = "Metformin",
                GenericName = "Metformin hydrochloride",
                Strength = "850 mg",
                Form = "Tablet",
                Description = "Biguanide used to improve glucose control in type 2 diabetes."
            }

        };

        foreach (var medication in medications)
        {
            medication.ImageUrl = imageUrl;
            medication.Status = MedicationStatus.Active;
            medication.ManufacturerId = null;
            medication.Barcode = null;
            medication.CreatedAt = seedDate;
            medication.UpdatedAt = seedDate;
        }

        modelBuilder.Entity<Medication>().HasData(medications);
    }
}
