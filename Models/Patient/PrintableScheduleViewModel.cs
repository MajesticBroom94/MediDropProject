namespace MediDropProject.Models.Patient;

public sealed class PrintableScheduleViewModel
{
    public string PatientName { get; set; } = "Patient";
    public long RoundId { get; set; }
    public DateTimeOffset GeneratedAtLocal { get; set; }
    public DateOnly StartDate { get; set; }
    public List<PrintableScheduleDayViewModel> Days { get; set; } = [];
}

public sealed class PrintableScheduleDayViewModel
{
    public DateOnly Date { get; set; }
    public List<PrintableScheduleSlotViewModel> Slots { get; set; } = [];
}

public sealed class PrintableScheduleSlotViewModel
{
    public int SlotNumber { get; set; }
    public string Period { get; set; } = "";
    public string? ScheduledTime { get; set; }
    public bool Cancelled { get; set; }
    public bool IsPast { get; set; }
    public bool IsMissed { get; set; }
    public string? ReportedStatus { get; set; }
    public List<PrintableScheduleMedicationViewModel> Medications { get; set; } = [];
}

public sealed class PrintableScheduleMedicationViewModel
{
    public string Name { get; set; } = "";
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int Quantity { get; set; }
}
