using Microsoft.AspNetCore.Identity;

namespace _2Korriku.Models;

public class ApplicationUser : IdentityUser
{
    [System.ComponentModel.DataAnnotations.StringLength(150)] public string? Specialization { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Coach = "Trajner";
    public const string Viewer = "Shikues";
    public const string Read = Admin + "," + Operator + "," + Viewer;
    public const string Write = Admin + "," + Operator;
    public static readonly string[] All = [Admin, Operator, Viewer, Coach];
}
