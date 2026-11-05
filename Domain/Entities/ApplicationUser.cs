using Microsoft.AspNetCore.Identity;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? StatusReason { get; set; }
    public DateTimeOffset? LastStatusChange { get; set; }
    public int? StatusChangedByUserId { get; set; }

    public LastSelectedProfile? LastSelectedProfile { get; set; } = Enums.LastSelectedProfile.Patient;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Patient? Patient { get; set; }
    public CareProvider? CareProvider { get; set; }
    public UserSetting? Settings { get; set; }


    public ICollection<AuditEvent> AuditEvents { get; set; } = [];
    public ICollection<SecurityEvent> SecurityEvents { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
