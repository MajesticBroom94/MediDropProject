using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class UserSetting
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Global schedule preferences.
    public TimeOnly MorningDispenseTime { get; set; } = new(8, 0);
    public TimeOnly MiddayDispenseTime { get; set; } = new(13, 0);
    public TimeOnly EveningDispenseTime { get; set; } = new(19, 0);
    public int? ReminderLeadMinutes { get; set; } = 15;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}