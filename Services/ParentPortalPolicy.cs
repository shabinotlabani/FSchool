using System.Security.Cryptography;
using System.Text;

namespace _2Korriku.Services;

public static class ParentPortalPolicy
{
    public static bool NeedsAcknowledgement(decimal debt, bool acknowledged)
        => debt > 0 && !acknowledged;

    // Demonstration identifier only, NOT an access credential for a real gate.
    // Stable per student/day, including repeated clicks, devices and link regeneration.
    public static string TicketId(int studentId, DateOnly date)
        => "PARK-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"2korriku-parking-v1:{studentId}:{date:yyyy-MM-dd}")))[..24];
    public static string QrPayload(string ticketId, DateOnly date)
        => $"2KORRIKU:PARKING:{date:yyyy-MM-dd}:{ticketId}";
}
