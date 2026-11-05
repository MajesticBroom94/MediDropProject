using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

public sealed class PatientDashboardViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public int TodayScheduledCount { get; set; }
    public int ActiveMedicationCount { get; set; }
    public int ActivePrescriptionCount { get; set; }
    public int CareProviderCount { get; set; }

    public bool CanConfirmDispenseRemotely { get; set; }
    public DashboardDoseViewModel? NextDose { get; set; }
    public List<DashboardDoseViewModel> TodayRoutine { get; set; } = [];
    public DashboardDeviceViewModel Device { get; set; } = new();
    public List<DashboardNotificationViewModel> RecentNotifications { get; set; } = [];
}

public sealed class DashboardDoseViewModel
{
    public long SlotId { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public MedicationRoundSlotStatus Status { get; set; }
    public DateTimeOffset? DispensedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public List<DashboardDoseItemViewModel> Items { get; set; } = [];

    public string PeriodName => ScheduledFor.Hour switch
    {
        < 12 => "Morning",
        < 18 => "Midday",
        _ => "Evening"
    };
}

public sealed class DashboardDoseItemViewModel
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int Quantity { get; set; }

    public string DisplayName => string.Join(" ", new[] { MedicationName, Strength, Form }
        .Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class DashboardDeviceViewModel
{
    public bool IsPaired { get; set; }
    public string? Name { get; set; }
    public string? DeviceUid { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public OperatingState? OperatingState { get; set; }
    public bool IsOnline { get; set; }
}

public sealed class DashboardNotificationViewModel
{
    public long Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsUnread { get; set; }
}

public sealed class PatientHistoryViewModel
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int EligibleOperationCount { get; set; }
    public int RemovedCount { get; set; }
    public int DispensedCount { get; set; }
    public int MissedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal CompletionRate { get; set; }
    public List<HistoryRoundViewModel> Rounds { get; set; } = [];
}

public sealed class HistoryRoundViewModel
{
    public long RoundId { get; set; }
    public MedicationRoundStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public int RemovedCount { get; set; }
    public int MissedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal CompletionRate { get; set; }
}

public sealed class RoundHistoryDetailsViewModel
{
    public long RoundId { get; set; }
    public MedicationRoundStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceUid { get; set; } = string.Empty;
    public int EligibleOperationCount { get; set; }
    public int RemovedCount { get; set; }
    public int DispensedCount { get; set; }
    public int MissedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal CompletionRate { get; set; }
    public List<RoundHistorySlotViewModel> Slots { get; set; } = [];
}

public sealed class RoundHistorySlotViewModel
{
    public long SlotId { get; set; }
    public Guid OperationId { get; set; }
    public int DeviceSlotNumber { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? DispensedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public MedicationRoundSlotStatus Status { get; set; }
    public List<RoundHistoryItemViewModel> Items { get; set; } = [];

    public string PeriodName => ScheduledFor.Hour switch
    {
        < 12 => "Morning",
        < 18 => "Midday",
        _ => "Evening"
    };
}

public sealed class RoundHistoryItemViewModel
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int Quantity { get; set; }

    public string DisplayName => string.Join(" ", new[] { MedicationName, Strength, Form }
        .Where(x => !string.IsNullOrWhiteSpace(x)));
}
