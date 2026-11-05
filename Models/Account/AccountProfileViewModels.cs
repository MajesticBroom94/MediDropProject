using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Models.Account;

public class EditProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";
    public bool IsProvider { get; set; }

    [StringLength(150)]
    [Display(Name = "Professional title")]
    public string? ProfessionalTitle { get; set; }

    [StringLength(2000)]
    public string? Bio { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}

public class ChangeAccountPasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match."), DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}
