using Microsoft.AspNetCore.Identity;

namespace _2Korriku.Models;

public class ApplicationUser : IdentityUser
{
    public int ClubId { get; set; }
    public bool IsPlatformOwner { get; set; }
    public bool CanVerifyBank { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(150)] public string? Specialization { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Coach = "Trajner";
    public const string Doctor = "Mjek";
    public const string Nutritionist = "Nutricist";
    public const string Psychologist = "Psikolog";
    public const string Viewer = "Shikues";
    public const string FieldOperator = "RezervimePrivate";
    public const string FieldRead = Read + "," + FieldOperator;
    public const string FieldWrite = Write + "," + FieldOperator;
    public static string Label(string role) => role == SuperAdmin ? "Super Admin" : role == FieldOperator ? "Rezervimet private" : role;
    public const string Read = Admin + "," + Operator + "," + Viewer;
    public const string Write = Admin + "," + Operator;
    // Super administrators also hold Admin, so existing operational permissions remain intact.
    public static readonly string[] All = [SuperAdmin, Admin, Operator, Viewer, Coach, FieldOperator, Doctor, Nutritionist, Psychologist];
}
