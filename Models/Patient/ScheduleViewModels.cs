using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

public sealed class ScheduleViewModel
{
    public bool ScheduleExists { get; set; }
    public bool CanApply { get; set; }
    public long? RoundId { get; set; }
    public DateOnly StartDate { get; set; }
    public int? CurrentScheduleVersion { get; set; }
    
    public bool DeviceSchedulePending { get; set; }

    [Required]
    public TimeOnly MorningTime { get; set; } = new(8, 0);

    [Required]
    public TimeOnly MiddayTime { get; set; } = new(13, 0);

    [Required]
    public TimeOnly EveningTime { get; set; } = new(19, 0);

    [Range(5, 120)]
    public int? ReminderLeadMinutes { get; set; } = 15;

    public List<ScheduleSlotViewModel> Slots { get; set; } = [];
    public List<ScheduleMedicationOptionViewModel> MedicationOptions { get; set; } = [];
}

public sealed class ScheduleSlotViewModel
{
    public long? ExistingSlotId { get; set; }
    public int DeviceSlotNumber { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public MedicationRoundSlotStatus Status { get; set; }
    public bool Cancelled { get; set; }
    public bool IsEditable { get; set; }
    public bool HadCommittedMedication { get; set; }
    public bool IsPastForNewSchedule { get; set; }
    public List<ScheduleItemViewModel> Items { get; set; } = [];

    public int DayIndex => (DeviceSlotNumber - 1) / 3;
    public int PeriodIndex => (DeviceSlotNumber - 1) % 3;
    public string PeriodKey => PeriodIndex switch { 0 => "morning", 1 => "midday", _ => "evening" };
    public string PeriodName => PeriodIndex switch { 0 => "Morning", 1 => "Midday", _ => "Evening" };
}

public sealed class ScheduleItemViewModel
{
    public long? ExistingItemId { get; set; }

    // Zero means that one of the fixed editor rows was left blank.
    // The controller ignores blank rows and validates selected medication server-side.
    public long PatientMedicationId { get; set; }

    public int Quantity { get; set; }

    public int OriginalQuantity { get; set; }
    public bool IsCommitted { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
}

public sealed class ScheduleMedicationOptionViewModel
{
    public long PatientMedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int AvailableQuantity { get; set; }

    public string DisplayName => string.Join(" ", new[] { Name, Strength, Form }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
