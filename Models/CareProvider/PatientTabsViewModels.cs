using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.CareProvider;

public class PatientTabBaseViewModel
{
    public int PatientId { get; set; }
    public long RelationshipId { get; set; }
    public string FullName { get; set; } = "Patient";
    public string Email { get; set; } = "";
    public string Initials { get; set; } = "P";
    public DateTimeOffset? ConnectedSince { get; set; }
}

public sealed class PatientOverviewViewModel : PatientTabBaseViewModel
{
    public int ScheduledCount { get; set; }
    public int RemovedCount { get; set; }
    public int AssessedCount { get; set; }
    public int MissedCount { get; set; }
    public int CancelledCount { get; set; }
    public double? AdherencePercent { get; set; }
    public int MedicationCount { get; set; }
    public int ActivePrescriptionCount { get; set; }
    public long? ActiveRoundId { get; set; }

    public List<RecentMedicationSlotViewModel> RecentSlots { get; set; } = [];
    public List<RecentPrescriptionViewModel> RecentPrescriptions { get; set; } = [];
}

public sealed class RecentMedicationSlotViewModel
{
    public DateTimeOffset ScheduledFor { get; set; }
    public MedicationRoundSlotStatus Status { get; set; }
    public List<string> MedicationNames { get; set; } = [];
}

public sealed class RecentPrescriptionViewModel
{
    public long Id { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public PrescriptionStatus Status { get; set; }
}

public sealed class PatientAdherenceViewModel : PatientTabBaseViewModel
{
    public int Days { get; set; } = 30;
    public int Scheduled { get; set; }
    public int Removed { get; set; }
    public int Cancelled { get; set; }
    public int Missed { get; set; }
    public int Pending { get; set; }
    public int Dispensed { get; set; }
    public int Assessed => Removed + Cancelled + Missed;
    public double? Rate => Assessed == 0 ? null : Math.Round(100.0 * Removed / Assessed, 1);
    public List<PatientSlotRowViewModel> Slots { get; set; } = [];
}

public sealed class PatientSlotRowViewModel
{
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? DispensedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public MedicationRoundSlotStatus Status { get; set; }
    public List<string> Medications { get; set; } = [];
}

public sealed class PatientMedicationsTabViewModel : PatientTabBaseViewModel
{
    public List<PatientMedicationRowViewModel> Medications { get; set; } = [];
}

public sealed class PatientMedicationRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public PatientMedicationStatus Status { get; set; }
    public DateTimeOffset? LastDispensedAt { get; set; }
}

public sealed class PatientScheduleViewModel : PatientTabBaseViewModel
{
    public List<PatientRoundViewModel> Rounds { get; set; } = [];
}

public sealed class PatientRoundViewModel
{
    public long Id { get; set; }
    public MedicationRoundStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<PatientSlotRowViewModel> Slots { get; set; } = [];
}

public sealed class PatientPrescriptionsViewModel : PatientTabBaseViewModel
{
    public List<PatientPrescriptionRowViewModel> Prescriptions { get; set; } = [];
}

public sealed class PatientPrescriptionRowViewModel
{
    public long Id { get; set; }
    public string Number { get; set; } = "";
    public PrescriptionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public int RepeatCount { get; set; }
    public int CollectedCount { get; set; }
    public List<string> Items { get; set; } = [];
}

public sealed class PatientPrescriptionDetailsViewModel : PatientTabBaseViewModel
{
    public PatientPrescriptionRowViewModel Prescription { get; set; } = new();
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Instructions { get; set; }
    public string? Notes { get; set; }
    public List<PatientPrescriptionItemViewModel> Items { get; set; } = [];
    public List<PatientPrescriptionCollectionViewModel> Collections { get; set; } = [];
}

public sealed class PatientPrescriptionCollectionViewModel
{
    public int FillNumber { get; set; }
    public DateTimeOffset CollectedAt { get; set; }
}

public sealed class PatientPrescriptionItemViewModel
{
    public string Name { get; set; } = "";
    public decimal DosageAmount { get; set; }
    public string DosageUnit { get; set; } = "";
    public string? Frequency { get; set; }
    public int? QuantityPrescribed { get; set; }
    public string? Instructions { get; set; }
}

public sealed class CreatePatientPrescriptionViewModel : PatientTabBaseViewModel
{
    [DataType(DataType.Date)] 
    public DateOnly? StartDate { get; set; }
    
    [DataType(DataType.Date)] 
    public DateOnly? EndDate { get; set; }
    
    [Range(0, 100)] 
    public int RepeatCount { get; set; }
    
    [StringLength(4000)] 
    public string? Instructions { get; set; }
    
    [StringLength(4000)] 
    public string? Notes { get; set; }
    
    [Required, MinLength(1)] 
    public List<PrescriptionItemInputViewModel> Items { get; set; } = [new()];
    
    public List<PrescriptionMedicineOptionViewModel> AvailableMedications { get; set; } = [];
}

public sealed class PrescriptionItemInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a medication.")]
    public int MedicationId { get; set; }

    [Range(
        typeof(decimal),
        "0.0001",
        "999999",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Enter a positive dosage."
    )]
    public decimal DosageAmount { get; set; }

    [Required, StringLength(50)] 
    public string DosageUnit { get; set; } = "";
    
    [StringLength(200)] 
    public string? Frequency { get; set; }
    
    [Range(1, int.MaxValue)] 
    public int? QuantityPrescribed { get; set; }
    
    [StringLength(4000)] 
    public string? Instructions { get; set; }
}

public sealed class PrescriptionMedicineOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}