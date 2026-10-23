using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class ClubTenant
{
    [Required, StringLength(30)] public string Sport { get; set; } = "Football";
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public DateOnly? ExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ClubTenantInput
{
    [Required, StringLength(30)] public string Sport { get; set; } = "Football";
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required] public DateOnly? ExpiresOn { get; set; } = _2Korriku.Services.BillingClock.Today.AddYears(1);
    public bool IsActive { get; set; } = true;
}
