using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Models.Account;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
