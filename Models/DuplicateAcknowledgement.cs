using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class DuplicateAcknowledgement
{
    public bool ConfirmDuplicate { get; set; }
    [StringLength(64)] public string? DuplicateToken { get; set; }
}
