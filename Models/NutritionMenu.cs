using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Models;

[Index(nameof(Scope),IsUnique=true)]
public class NutritionMenu
{
    [MaxLength(20)] public string? ApprovedRole { get; set; }
    public int Id { get; set; }
    // Zero is the shared menu; positive values are student IDs.
    public int Scope { get; set; }
    public string Data { get; set; } = "";
    [MaxLength(2000)] public string Recommendations { get; set; } = "";
    [MaxLength(450)] public string UpdatedBy { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public Guid Revision { get; set; }
}
public class MenuDay
{
    [Required,MaxLength(500)] public string Breakfast { get; set; } = "";
    [Required,MaxLength(500)] public string Lunch { get; set; } = "";
    [Required,MaxLength(500)] public string Dinner { get; set; } = "";
    [Required,MaxLength(500)] public string Snack { get; set; } = "";
}
public class MenuInput
{
    public string? ApprovedRole { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DraftReferralId { get; set; }
    [Range(0,int.MaxValue)] public int StudentId { get; set; }
    public Guid Revision { get; set; }
    public List<MenuDay> Days { get; set; } = [];
    [MaxLength(2000)] public string Recommendations { get; set; } = "";
    [MaxLength(2000)] public string? AiInstructions { get; set; }
    public bool AllergiesReviewed { get; set; }
    public bool Individual { get; set; }
    public static readonly string[] DayNames=["E hënë","E martë","E mërkurë","E enjte","E premte","E shtunë","E diel"];
}
public class CareReferral
{
    public bool MenuForCoach { get; set; }
    [MaxLength(20)] public string? MenuStatus { get; set; }
    public int MenuAttempts { get; set; }
    public DateTime? MenuNextAttempt { get; set; }
    public string? MenuDraft { get; set; }
    [MaxLength(500)] public string? MenuError { get; set; }
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(450)] public string CoachId { get; set; } = "";
    [MaxLength(20)] public string Kind { get; set; } = "";
    [Required,MaxLength(2000)] public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    [MaxLength(450)] public string? ClosedBy { get; set; }
}
public record CoachMenuPage(Student Player,MenuInput Current,CareReferral? Proposal,MenuInput? Draft);
public class ReferralInput
{
    public int StudentId { get; set; }
    [Required,RegularExpression("Mjek|Nutricist")] public string Kind { get; set; } = "Mjek";
    [Required,MaxLength(2000)] public string Reason { get; set; } = "";
}
public static class CareMenuStatus
{
    public static string Label(string? status)=>status switch {"Pending"=>"Menuja në pritje","Processing"=>"AI po përgatit menunë","Ready"=>"Propozimi gati","Failed"=>"Gjenerimi nuk u përfundua","Published"=>"Menuja u publikua",_=>""};
}
