using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public PatientStatus Status { get; set; }
    
    [StringLength(500)]
    public string? StatusReason { get; set; }

    public DateTimeOffset LastStatusChange { get; set; }
    public int? StatusChangedByUserId { get; set; }
    
    public ApplicationUser User { get; set; } = null!;

    public ICollection<PatientMedication> Medications { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
    public ICollection<PatientCareRelationship> CareRelationships { get; set; } = [];
    public ICollection<DeviceAssignment> DeviceAssignments { get; set; } = [];
}

