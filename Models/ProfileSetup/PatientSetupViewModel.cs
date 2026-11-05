using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Models.Setup;

public class PatientSetupViewModel
{
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
}
