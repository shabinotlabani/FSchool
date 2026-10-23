namespace _2Korriku.Services;

// One scope represents exactly one club. Zero deliberately grants no business access.
public sealed class ClubContext
{
    public int ClubId { get; set; }
    public bool IdentityAccess { get; set; }
    public bool ManagePlatform { get; set; }
    public bool IsOwner { get; set; }
    public string? UserId { get; set; }
    public const string CookieName = "UPNEXT.Club";
}
