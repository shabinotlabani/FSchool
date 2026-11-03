using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class ClubRegistrationInput
{
    [Required(ErrorMessage = "Shkruani emrin e klubit."), StringLength(100)]
    [Display(Name = "Emri i klubit")]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "Shkruani llojin e aktivitetit."), StringLength(100)]
    [Display(Name = "Lloji i aktivitetit")]
    public string Activity { get; set; } = "";
    [Required(ErrorMessage = "Shkruani emailin."), EmailAddress(ErrorMessage = "Shkruani një email të vlefshëm."), StringLength(254)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Shkruani numrin e kontaktit."), StringLength(30), RegularExpression(@"\+?[0-9][0-9 ()\-]{5,28}", ErrorMessage = "Kontrolloni numrin e kontaktit.")]
    [Display(Name = "Numri i kontaktit")]
    public string Phone { get; set; } = "";
}

public class ClubRegistration : ClubRegistrationInput
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
