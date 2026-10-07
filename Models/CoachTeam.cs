namespace _2Korriku.Models;
public class CoachTeam
{
 public string UserId { get; set; } = "";
 public ApplicationUser? User { get; set; }
 public int TrainingTeamId { get; set; }
 public TrainingTeam? TrainingTeam { get; set; }
}
public class CoachDirectoryModel
{
 public List<ApplicationUser> Coaches { get; set; } = [];
 public List<CoachTeam> Assignments { get; set; } = [];
}
