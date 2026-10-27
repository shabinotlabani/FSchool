namespace _2Korriku.Models;
public class StudentPhoto
{
 public int Id { get; set; }
 public int StudentId { get; set; }
 public Student Student { get; set; } = null!;
 public byte[] Data { get; set; } = [];
 public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
