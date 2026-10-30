using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class GeminiFixtureService(HttpClient http,IConfiguration config,ApplicationDbContext db,ILogger<GeminiFixtureService> logger)
{
    private sealed class AiResult { public List<AiFixture> Fixtures { get; set; }=[]; }
    private sealed class AiFixture { public string Opponent { get; set; }=""; public string Date { get; set; }=""; public string? Kickoff { get; set; } public bool Home { get; set; } public string? Venue { get; set; } public string? Stage { get; set; } public string SourceUrl { get; set; }=""; }

    public async Task<MatchFixtureProposalModel> Propose(int teamId,int leagueId,DateOnly from,CancellationToken ct)
    {
        var team=await db.TrainingTeams.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==teamId&&x.IsActive,ct)??throw new ValidationException("Zgjidhni ekipin.");
        var league=await db.Leagues.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==leagueId&&x.IsActive&&x.Teams.Any(t=>t.TrainingTeamId==teamId),ct)??throw new ValidationException("Zgjidhni garën ku është regjistruar ekipi.");
        if(from<league.StartsOn)from=league.StartsOn;if(from>league.EndsOn)throw new ValidationException("Sezoni i kësaj gare ka përfunduar.");
        var settings=await db.ClubSettings.AsNoTracking().SingleOrDefaultAsync(ct);
        var club=settings?.DisplayName??settings?.Name??team.Name;
        var key=config["Gemini:ApiKey"];if(string.IsNullOrWhiteSpace(key))throw new ValidationException("Gemini nuk është konfiguruar në server.");
        var model=config["Gemini:MatchModel"]??config["Gemini:MenuModel"]??"gemini-3.5-flash-lite";
        var prompt=$"Kërko vetëm orarin zyrtar të publikuar për klubin '{club}', ekipin '{team.Name}', në garën '{league.Name}' ({league.Organizer}), sezoni '{league.Season}', nga {from:yyyy-MM-dd} deri {league.EndsOn:yyyy-MM-dd}. Mos përfshi rezultate të kaluara, ndeshje të ekipit tjetër me emër të ngjashëm ose ndeshje pa burim zyrtar. Nëse orari nuk është publikuar, kthe fixtures bosh. Për çdo ndeshje jep kundërshtarin pa emrin e klubit tonë, datën, orën në kohën lokale nëse është publikuar, vendas/mysafir, stadiumin, fazën dhe URL-në direkte të burimit zyrtar. Vetëm JSON: {{\"fixtures\":[{{\"opponent\":\"\",\"date\":\"YYYY-MM-DD\",\"kickoff\":\"HH:mm ose null\",\"home\":true,\"venue\":null,\"stage\":null,\"sourceUrl\":\"https://...\"}}]}}";
        using var request=new HttpRequestMessage(HttpMethod.Post,$"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");request.Headers.Add("x-goog-api-key",key);
        request.Content=JsonContent.Create(new{contents=new[]{new{role="user",parts=new[]{new{text=prompt}}}},tools=new[]{new{googleSearch=new{}}},generationConfig=new{responseMimeType="application/json",maxOutputTokens=4000,thinkingConfig=new{thinkingLevel="minimal"}}});
        try{
            using var response=await http.SendAsync(request,ct);if(!response.IsSuccessStatusCode)throw new ValidationException(GeminiMenuService.ErrorMessage(response.StatusCode));
            using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));var candidate=doc.RootElement.GetProperty("candidates")[0];
            var text=string.Concat(candidate.GetProperty("content").GetProperty("parts").EnumerateArray().Where(x=>x.TryGetProperty("text",out _)).Select(x=>x.GetProperty("text").GetString()));
            var result=JsonSerializer.Deserialize<AiResult>(text,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??new();
            var rows=new List<MatchFixtureProposal>();
            foreach(var x in result.Fixtures.Take(60))if(DateOnly.TryParse(x.Date,out var date)&&date>=from&&date<=league.EndsOn&&x.Opponent.Trim().Length is >0 and <=150&&OfficialSource(x.SourceUrl,league.Organizer)){
                TimeOnly? time=TimeOnly.TryParse(x.Kickoff,out var parsed)?parsed:null;rows.Add(new(){Index=rows.Count,Opponent=x.Opponent.Trim(),Date=date,Kickoff=time,Home=x.Home,Venue=Trim(x.Venue,150),Stage=Trim(x.Stage,30),SourceUrl=x.SourceUrl});
            }
            rows=rows.GroupBy(x=>new{x.Date,x.Kickoff,Name=x.Opponent.ToUpper()}).Select(x=>x.First()).OrderBy(x=>x.Date).ThenBy(x=>x.Kickoff).ToList();for(var i=0;i<rows.Count;i++)rows[i].Index=i;
            logger.LogInformation("Gemini proposed {Count} verified fixtures for league {LeagueId}",rows.Count,leagueId);
            return new(){TeamId=teamId,LeagueId=leagueId,From=from,TeamName=team.Name,LeagueName=league.Name,Fixtures=rows,Selected=rows.Select(x=>x.Index).ToList()};
        }catch(JsonException){throw new ValidationException("Përgjigjja e Gemini ishte e paplotë. Provoni përsëri.");}catch(HttpRequestException){throw new ValidationException("Lidhja me Gemini dështoi. Provoni përsëri.");}catch(TaskCanceledException){throw new ValidationException("Kërkimi zgjati shumë. Provoni përsëri.");}
    }
    private static string? Trim(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim()[..Math.Min(value.Trim().Length,max)];
    public static bool OfficialSource(string value,string organizer)=>Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&(organizer.ToUpperInvariant() switch{"UEFA"=>uri.Host.EndsWith("uefa.com"),"FIFA"=>uri.Host.EndsWith("fifa.com"),_=>uri.Host.EndsWith("ffk-kosova.com")||uri.Host.EndsWith("ffk-ks.com")});
}
