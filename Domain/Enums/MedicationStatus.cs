using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Domain.Enums;

public enum MedicationStatus
{
    Pending,
    Active,
    Duplicated,
    Discontinued,
    [Display(Name="Not Supported")]
    NotSupported
}