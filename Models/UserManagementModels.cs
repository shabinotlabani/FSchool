using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class UserEditModel : IValidatableObject
{
    public string? Id { get; set; }
    [Phone, StringLength(50)] public string? Phone { get; set; }
    [StringLength(150)] public string? Specialization { get; set; }
    public List<int> TeamIds { get; set; } = [];
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public List<TrainingTeam> Teams { get; set; } = [];
    [Required(ErrorMessage = "Shkruani emrin."), StringLength(100)] public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Shkruani mbiemrin."), StringLength(100)] public string LastName { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Shkruani një email të vlefshëm."), StringLength(256)] public string Email { get; set; } = "";
    [Required, RegularExpression("Admin|Operator|Shikues|Trajner", ErrorMessage = "Zgjidhni një rol të vlefshëm.")] public string Role { get; set; } = Roles.Operator;
    public bool IsActive { get; set; } = true;
    [DataType(DataType.Password), StringLength(100, MinimumLength = 6, ErrorMessage = "Fjalëkalimi duhet të ketë 6–100 karaktere.")] public string? Password { get; set; }
    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Fjalëkalimet nuk përputhen.")] public string? ConfirmPassword { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Password)) yield return new("Fjalëkalimi kërkohet për shfrytëzuesin e ri.", [nameof(Password)]);
        if (!string.IsNullOrEmpty(Password) && string.IsNullOrEmpty(ConfirmPassword)) yield return new("Përsëritni fjalëkalimin.", [nameof(ConfirmPassword)]);
    }
}

public class UserListModel
{
    public ApplicationUser User { get; set; } = new();
    public string Role { get; set; } = "Pa rol";
    public bool IsActive => !Services.UserAdministrationService.IsDisabled(User);
}
