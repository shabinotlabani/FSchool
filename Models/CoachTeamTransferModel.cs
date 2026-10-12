using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace _2Korriku.Models;

public class CoachTeamTransferModel
{
    [Range(1,int.MaxValue)] public int StudentId { get; set; }
    [Range(1,int.MaxValue)] public int SourceTeamId { get; set; }
    [Required,RegularExpression("head|assistant")] public string CoachRole { get; set; }="head";
    [Required,Range(1,int.MaxValue)] public int? TeamId { get; set; }
    [ValidateNever] public string StudentName { get; set; }="";
    [ValidateNever] public string SourceTeamName { get; set; }="";
    [ValidateNever] public int Age { get; set; }
    [ValidateNever] public List<TeamOption> Teams { get; set; }=[];
}
