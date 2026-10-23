using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class CoachMessage
{
    public long Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Required] public string CoachId { get; set; } = "";
    public ApplicationUser Coach { get; set; } = null!;
    [Required, StringLength(2000)] public string Body { get; set; } = "";
    public bool FromParent { get; set; }
    public Guid RequestId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public class ChatInput
{
    public int StudentId { get; set; }
    public string CoachId { get; set; } = "";
    [Required(ErrorMessage="Shkruani mesazhin."), StringLength(2000, ErrorMessage="Mesazhi lejohet deri në 2000 karaktere.")]
    public string Body { get; set; } = "";
    public Guid RequestId { get; set; } = Guid.NewGuid();
}
public record ChatCoach(string Id, string Name);
public class ChatView
{
    public Student Student { get; set; } = null!;
    public string CoachId { get; set; } = "";
    public string CoachName { get; set; } = "";
    public string? Token { get; set; }
    public bool Parent => Token != null;
    public List<ChatCoach> Coaches { get; set; } = [];
    public List<CoachMessage> Messages { get; set; } = [];
    public ChatInput Input { get; set; } = new();
    public bool HasOlder { get; set; }
    public long? Before { get; set; }
}
public class CoachInboxRow
{
    public int StudentId { get; set; }
    public string Name { get; set; } = "";
    public string? ParentName { get; set; }
    public string Body { get; set; } = "";
    public DateTime Date { get; set; }
    public int Unread { get; set; }
}
public class CoachDeactivateInput
{
    public int StudentId { get; set; }
    public Guid Revision { get; set; }
    [Required(ErrorMessage="Shkruani arsyen."), StringLength(200)] public string Reason { get; set; } = "";
    public string Name { get; set; } = "";
    public int TeamId { get; set; }
}
