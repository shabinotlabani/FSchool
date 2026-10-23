using System.ComponentModel.DataAnnotations;
using System.Globalization;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class SportDevelopmentService(ApplicationDbContext db)
{
    public static readonly Dictionary<string,string> Sports=new(){["Football"]="Futboll",["Basketball"]="Basketboll",["Volleyball"]="Volejboll",["Handball"]="Hendboll",["Tennis"]="Tenis",["Other"]="Sport tjetër"};
    public static string Label(string sport)=>Sports.GetValueOrDefault(sport,sport);
    public Task<string> Sport()=>db.Clubs.Where(x=>x.Id==db.CurrentClubId).Select(x=>x.Sport).SingleAsync();
    public static List<DevelopmentSection> Defaults(string sport)
    {
        if(sport=="Football")return DevelopmentSections.All.ToList();
        var sections=DevelopmentSections.All.Where(x=>x.Key is "Measurements" or "Testing" or "Load" or "Talent").ToList();
        string[] positions=sport switch{
            "Basketball"=>["Organizator (PG)","Fundor (SG)","Ala (SF)","Ala e fortë (PF)","Qendër (C)"],
            "Volleyball"=>["Pasues","Diagonal","Sulmues anësor","Bllokues qendror","Libero"],
            "Handball"=>["Krah i majtë","Krah i djathtë","Qendër","Anësor i majtë","Anësor i djathtë","Pivot"],
            "Tennis"=>["Tek","Dyshe"],_=>["Pa përcaktuar"]};
        sections.Add(new("Profile","Profili i lojtarit",[
            new("PlayerRole","Roli",Choices:sport=="Handball"?["Lojtar","Portier"]:["Lojtar"]),
            new("PrimaryPosition","Pozita kryesore",Choices:["Pa caktuar",..positions]),new("SecondaryPosition","Pozita e dytë",Choices:["Asnjë",..positions]),
            new("Hand","Dora dominante",Choices:["Djathta","Majta","Të dyja"]),
            new("Focus","Përqendrimi / 5",1,5,Slider:true),new("Teamwork","Bashkëpunimi / 5",1,5,Slider:true)]));
        DevelopmentField R(string key,string label)=>new(key,label+" / 5",1,5,Slider:true);
        sections.Add(new("Skills","Aftësitë teknike",sport switch{
            "Basketball"=>[R("Shooting","Gjuajtja"),R("Dribbling","Driblimi"),R("Passing","Pasimet"),R("Layup","Hapat dhe përfundimi"),R("Rebounding","Ribaundet"),R("Defense","Mbrojtja")],
            "Volleyball"=>[R("Serve","Shërbimi"),R("Reception","Pranimi"),R("Setting","Pasimi"),R("Attack","Sulmi"),R("Block","Blloku"),R("Defense","Mbrojtja")],
            "Handball"=>[R("Shooting","Gjuajtja"),R("Passing","Pasimet"),R("Dribbling","Driblimi"),R("Feint","Fintat"),R("Defense","Mbrojtja")],
            "Tennis"=>[R("Serve","Shërbimi"),R("Forehand","Forehand"),R("Backhand","Backhand"),R("Volley","Voleja"),R("Return","Kthimi i shërbimit"),R("Footwork","Lëvizja e këmbëve")],
            _=>[R("Technique","Teknika"),R("Coordination","Koordinimi"),R("Accuracy","Saktësia")]}));
        sections.Add(new("Tactics","Aftësitë taktike",[R("Decisions","Vendimmarrja"),R("Positioning","Pozicionimi"),R("GameReading","Leximi i lojës")]));
        var match=new List<DevelopmentField>{new("Opponent","Kundërshtari"),new("Competition","Gara")};
        if(sport=="Basketball")match.AddRange([new("Points","Pikë",0,200),new("Rebounds","Ribaunde",0,100),new("Assists","Asistime",0,100),new("Steals","Topa të vjedhur",0,100),new("Blocks","Blloqe",0,100),new("Turnovers","Topa të humbur",0,100),new("Minutes","Minuta",0,100)]);
        else if(sport=="Volleyball")match.AddRange([new("Points","Pikë",0,150),new("Aces","Asa",0,100),new("Blocks","Blloqe",0,100),new("Errors","Gabime",0,100)]);
        else if(sport=="Handball")match.AddRange([new("Goals","Gola",0,100),new("Assists","Asistime",0,100),new("Saves","Pritje",0,100),new("Minutes","Minuta",0,100)]);
        else if(sport=="Tennis")match.AddRange([new("Aces","Asa",0,100),new("DoubleFaults","Gabime të dyfishta",0,100),new("Winners","Goditje fituese",0,200),new("Errors","Gabime të paforcuara",0,200)]);
        else match.Add(new("Result","Rezultati"));
        sections.Add(new("Match","Vëzhgimi i ndeshjes",match.ToArray()));
        if(sport=="Handball"){
            sections.Add(new("Goalkeeping","Portieri",[R("Reflexes","Reflekset"),R("Positioning","Pozicionimi"),R("Saves","Pritjet"),R("Distribution","Shpërndarja")]));
            sections.Add(new("KeeperTraining","Ushtrimet e portierit",[new("Drill","Ushtrimi"),new("Minutes","Kohëzgjatja (min)",1,120),R("Execution","Zbatimi")]));
        }
        return sections;
    }
    public async Task<List<DevelopmentMetric>> Fields(string sport)
    {
        var rows=await db.DevelopmentMetrics.AsNoTracking().Where(x=>x.Sport==sport).ToListAsync();
        var result=Defaults(sport).SelectMany(s=>s.Fields.Select((f,i)=>new DevelopmentMetric{Sport=sport,Section=s.Key,Key=f.Key,Label=f.Label,Min=f.Min,Max=f.Max,Step=f.Step,Type=f.Choices!=null?"Choice":f.Slider?(f.Min==1&&f.Max==5?"Rating":"Scale"):f.Min.HasValue?"Number":"Text",Choices=string.Join('\n',f.Choices??[]),SortOrder=i*10,Revision=Guid.Empty})).ToList();
        foreach(var row in rows){result.RemoveAll(x=>x.Section==row.Section&&x.Key==row.Key);result.Add(row);}
        return result.OrderBy(x=>x.Section).ThenBy(x=>x.SortOrder).ThenBy(x=>x.Key).ToList();
    }
    public async Task<SportCatalog> Catalog()
    {
        var sport=await Sport();var fields=await Fields(sport);
        return new(sport,Defaults(sport).Select(s=>new DevelopmentSection(s.Key,s.Label,fields.Where(x=>x.Section==s.Key&&x.Enabled).Select(ToField).ToArray())).Where(s=>s.Fields.Length>0).ToList());
    }
    public static DevelopmentField ToField(DevelopmentMetric m)=>new(m.Key,m.Label,m.Type is "Number" or "Rating" or "Scale"?m.Min:null,m.Type is "Number" or "Rating" or "Scale"?m.Max:null,m.Step,m.Type is "Rating" or "Scale",m.Type=="Choice"?m.Choices.Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries):null);
    public async Task Save(MetricInput input)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        var sport=await Sport();
        if(input.Sport!=sport||!Defaults(sport).Any(x=>x.Key==input.Section))throw new ValidationException("Sporti ose pjesa ka ndryshuar. Rihapni formularin.");
        if(input.Type is not ("Number" or "Rating" or "Scale" or "Text" or "Choice"))throw new ValidationException("Zgjidhni llojin e fushës.");
        if(input.Type=="Rating"){input.Min=1;input.Max=5;input.Step="1";}
        if(input.Type is "Number" or "Rating" or "Scale" && (!input.Min.HasValue||!input.Max.HasValue||input.Min<0||input.Max<=input.Min||!decimal.TryParse(input.Step,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var step)||step<=0))throw new ValidationException("Kontrolloni kufijtë dhe hapin e matjes.");
        var choices=(input.Choices??"").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct().ToArray();
        if(input.Type=="Choice"&&(choices.Length==0||choices.Length>100||choices.Any(x=>x.Length>150)))throw new ValidationException("Shkruani opsionet, një në çdo rresht.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20261024,{db.CurrentClubId})");
        if(await Sport()!=sport)throw new ValidationException("Sporti ka ndryshuar. Rihapni formularin.");
        DevelopmentMetric? row=input.Id==0?null:await db.DevelopmentMetrics.SingleOrDefaultAsync(x=>x.Id==input.Id&&x.Sport==sport);
        if(input.Id!=0&&(row==null||row.Revision!=input.Revision))throw new ValidationException("Fusha ka ndryshuar. Rihapeni.");
        if(row==null){
            if(input.Key.Length>0){
                if(!Defaults(sport).Any(s=>s.Key==input.Section&&s.Fields.Any(f=>f.Key==input.Key)))throw new ValidationException("Fusha nuk u gjet.");
                if(await db.DevelopmentMetrics.AnyAsync(x=>x.Sport==sport&&x.Section==input.Section&&x.Key==input.Key))throw new ValidationException("Fusha ka ndryshuar. Rihapeni.");
            }
            row=new(){Sport=sport,Section=input.Section,Key=input.Key.Length==0?"Custom"+Guid.NewGuid().ToString("N"):input.Key};db.DevelopmentMetrics.Add(row);
        }
        // System profile fields remain choices so position and role forms stay valid.
        if(row.Section=="Profile"&&row.Key is "PlayerRole" or "PrimaryPosition" or "SecondaryPosition"){
            if(input.Type!="Choice"||!input.Enabled)throw new ValidationException("Roli dhe pozitat duhet të mbeten lista aktive.");
            if(row.Key=="PlayerRole"&&choices.Contains("Portier")&&!Defaults(sport).Any(s=>s.Key=="Goalkeeping"))throw new ValidationException("Ky sport nuk ka rol portieri.");
            if(row.Key=="PrimaryPosition"&&!choices.Contains("Pa caktuar"))choices=["Pa caktuar",..choices];
            if(row.Key=="SecondaryPosition"&&!choices.Contains("Asnjë"))choices=["Asnjë",..choices];
        }
        row.Label=input.Label.Trim();row.Type=input.Type;row.Min=input.Min;row.Max=input.Max;row.Step=input.Step;
        row.Choices=string.Join('\n',choices);row.Enabled=input.Enabled;row.SortOrder=input.SortOrder;row.Revision=Guid.NewGuid();
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
