using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class ParentPushSettings
{
    public int Id { get; set; }
    [MaxLength(300)] public string Subject { get; set; } = "";
    [MaxLength(100)] public string PublicKey { get; set; } = "";
    [MaxLength(100)] public string PrivateKey { get; set; } = "";
}
public class ParentPushSubscription
{
    public int? FamilyId { get; set; }
    public bool Enabled { get; set; } = true;
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(64)] public string TokenHash { get; set; } = "";
    [MaxLength(2048)] public string Endpoint { get; set; } = "";
    [MaxLength(64)] public string EndpointHash { get; set; } = "";
    [MaxLength(100)] public string P256dh { get; set; } = "";
    [MaxLength(30)] public string Auth { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class ParentPushMessage
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [MaxLength(80)] public string Title { get; set; } = "";
    [MaxLength(400)] public string Body { get; set; } = "";
    [MaxLength(150)] public string Audience { get; set; } = "";
    [MaxLength(450)] public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ParentPushDelivery> Deliveries { get; set; } = [];
}
public class ParentPushDelivery
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public ParentPushMessage Message { get; set; } = null!;
    public int SubscriptionId { get; set; }
    public ParentPushSubscription Subscription { get; set; } = null!;
    public int Attempts { get; set; }
    public DateTime NextAttempt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public bool Failed { get; set; }
}
public class ParentPushInput
{
    [Required,MaxLength(2048)] public string Endpoint { get; set; } = "";
    [MaxLength(100)] public string? P256dh { get; set; }
    [MaxLength(30)] public string? Auth { get; set; }
}
public class ParentNotificationInput
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public int? TeamId { get; set; }
    public int? StudentId { get; set; }
    [Required,StringLength(80)] public string Title { get; set; } = "";
    [Required,StringLength(400)] public string Body { get; set; } = "";
}
public class ParentNotificationsView
{
    public bool Configured { get; set; }
    public int Devices { get; set; }
    public List<TrainingTeam> Teams { get; set; } = [];
    public List<Student> Students { get; set; } = [];
    public List<ParentPushMessage> Messages { get; set; } = [];
    public ParentNotificationInput Input { get; set; } = new();
}
