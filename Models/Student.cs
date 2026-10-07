using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class Student
{
    public int Id { get; set; }
    public bool FirstMonthUsesWeeks { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [StringLength(150)]
    public string? ParentName { get; set; }

    [EmailAddress]
    public string? ParentEmail { get; set; }

    [Phone]
    public string? ParentPhone { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "numeric(18,2)")]
    public decimal MonthlyFee { get; set; }

    public int? GroupId { get; set; }
    public int? TrainingTeamId { get; set; }
    public TrainingTeam? TrainingTeam { get; set; }
    public int? CoachId { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
