using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class ClubSettings
{
    public int Id { get; set; } = 1;
    [Required, MaxLength(100)] public string Name { get; set; } = "2 Korriku";
    [Required, MaxLength(150)] public string DisplayName { get; set; } = "FC 2 Korriku";
    [Required, MaxLength(200)] public string LegalName { get; set; } = "Klubi Futbollistik 2 Korriku O.J.Q.";
    [MaxLength(50)] public string? RegistrationNumber { get; set; } = "5110633-4";
    [MaxLength(200)] public string? Address { get; set; }
    [MaxLength(100)] public string? City { get; set; } = "Prishtinë";
    [Range(1800, 2200)] public int? FoundedYear { get; set; } = 1957;
    [MaxLength(50)] public string? Phone { get; set; }
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    public byte[]? Logo { get; set; }
    [MaxLength(30)] public string? LogoType { get; set; }
    public string LocationLine => string.Join(" · ", new[] { City, FoundedYear?.ToString() }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
