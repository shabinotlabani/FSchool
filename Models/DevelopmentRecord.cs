using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class DevelopmentRecord
{
    [MaxLength(30)] public string Sport { get; set; } = "Football";
    public string SchemaData { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DevelopmentSection Schema => string.IsNullOrEmpty(SchemaData) ? DevelopmentSections.Find(Kind)! : JsonSerializer.Deserialize<DevelopmentSection>(SchemaData)!;
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(450)] public string CoachId { get; set; } = "";
    public ApplicationUser Coach { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "Profile";
    public DateOnly Date { get; set; }
    public string Data { get; set; } = "{}";
    [MaxLength(2000)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public Dictionary<string,string> Values => JsonSerializer.Deserialize<Dictionary<string,string>>(Data)!;
}
public class DevelopmentRecordInput
{
    public string Sport { get; set; } = "Football";
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Guid Revision { get; set; }
    public string Kind { get; set; } = "Profile";
    public DateOnly Date { get; set; } = BillingClock.Today;
    public Dictionary<string,string?> Values { get; set; } = new();
    [MaxLength(2000)] public string? Notes { get; set; }
}
public record DevelopmentField(string Key,string Label,decimal? Min=null,decimal? Max=null,string Step="1",bool Slider=false,string[]? Choices=null);
public record DevelopmentSection(string Key,string Label,DevelopmentField[] Fields);
public record DevelopmentRecordGrid(DevelopmentSection Section,List<DevelopmentRecord> Records,string CoachId);
public static class DevelopmentSections
{
    public static readonly string[] Positions=["Mbrojtës qendror","Mbrojtës i djathtë","Mbrojtës i majtë","Mesfushor defensiv","Mesfushor qendror","Mesfushor ofensiv","Krah i djathtë","Krah i majtë","Sulmues"];
    public static string? RoleLabel(string? role)=>role=="Lojtar fushe"?"Lojtar":role;
    public static readonly DevelopmentSection[] All = [
        new("Skills","Aftësitë teknike",[
            new("Passing","Pasimet / 5",1,5,Slider:true),new("Dribbling","Driblimi / 5",1,5,Slider:true),
            new("BallControl","Kontrolli i topit / 5",1,5,Slider:true),new("Decisions","Vendimmarrja / 5",1,5,Slider:true),
            new("Finishing","Finalizimi / 5",1,5,Slider:true),new("WeakFoot","Këmba jo-dominante / 5",1,5,Slider:true),
            new("Crossing","Krosimet / 5",1,5,Slider:true),new("Tackling","Marrja e topit / 5",1,5,Slider:true)]),
        new("Tactics","Aftësitë taktike",[
            new("Scanning","Vëzhgimi para marrjes së topit / 5",1,5,Slider:true),new("Positioning","Pozicionimi / 5",1,5,Slider:true),
            new("OffBall","Lëvizja pa top / 5",1,5,Slider:true),new("Pressing","Presingu / 5",1,5,Slider:true),
            new("Transition","Reagimi në tranzicion / 5",1,5,Slider:true),new("Defending","Mbrojtja 1 kundër 1 / 5",1,5,Slider:true)]),
        new("Goalkeeping","Portieri",[
            new("Handling","Kapja e topit / 5",1,5,Slider:true),new("ShotStopping","Pritjet / 5",1,5,Slider:true),
            new("OneOnOne","Daljet 1 kundër 1 / 5",1,5,Slider:true),new("Aerial","Topat në ajër / 5",1,5,Slider:true),
            new("Distribution","Shpërndarja e topit / 5",1,5,Slider:true),new("Communication","Komunikimi / 5",1,5,Slider:true)]),
        new("Match","Vëzhgimi i ndeshjes",[
 
            new("Opponent","Kundërshtari"),new("Competition","Gara"),new("Position","Pozita në ndeshje"),
            new("Minutes","Minuta të luajtura",0,150),new("Goals","Gola",0,30),new("Assists","Asistime",0,30),
            new("Shots","Goditje",0,100),new("OnTarget","Goditje në portë",0,100),
            new("Passes","Pasime të tentuara",0,300),new("Completed","Pasime të sakta",0,300),
            new("Duels","Duele",0,100),new("DuelsWon","Duele të fituara",0,100),new("Saves","Pritje të portierit",0,100)]),
        new("Load","Ngarkesa e stërvitjes",[
            new("Session","Lloji i seancës",Choices:["Stërvitje ekipore","Stërvitje individuale","Ndeshje","Rikuperim"]),
            new("Minutes","Kohëzgjatja (min)",1,300),new("RPE","Përpjekja e raportuar nga lojtari · RPE 0–10",0,10,Slider:true)]),
        new("KeeperTraining","Ushtrimet e portierit",[
            new("Drill","Ushtrimi",Choices:["Kapje e topave të ulët","Kapje e topave në lartësi","Pritje me zhytje majtas / djathtas","Pozicionim dhe mbyllje e këndit","Dalje 1 kundër 1","Dalje në krosime","Pasim i shkurtër nën presion","Shpërndarje e gjatë","Lëvizje e këmbëve dhe pozicioni bazë"]),
            new("Minutes","Kohëzgjatja (min)",1,120),new("Sets","Seri",1,30),new("Repetitions","Përsëritje për seri",1,100),
            new("Execution","Zbatimi / 5",1,5,Slider:true),new("Goal","Objektivi për seancën tjetër")]),
        new("Profile","Profili i lojtarit",[
            new("PlayerRole","Roli në fushë",Choices:["Lojtar","Portier"]),
            new("PrimaryPosition","Pozita kryesore",Choices:["Pa caktuar",..Positions]),
            new("SecondaryPosition","Pozita e dytë",Choices:["Asnjë",..Positions]),
            new("Position","Pozita në fushë"),new("Foot","Këmba dominante",Choices:["Djathta","Majta","Të dyja"]),
            new("Technique","Aftësitë teknike / 5",1,5,Slider:true),new("Tactics","Kuptimi taktik / 5",1,5,Slider:true),
            new("Physical","Përgatitja fizike / 5",1,5,Slider:true),new("Focus","Përqendrimi / 5",1,5,Slider:true),
            new("Confidence","Vetëbesimi / 5",1,5,Slider:true),new("Resilience","Reagimi ndaj vështirësive / 5",1,5,Slider:true)]),
        new("Measurements","Matjet fizike",[
            new("Weight","Pesha (kg)",10,250,"0.1"),new("Height","Gjatësia (cm)",70,250,"0.1"),
            new("ArmSpan","Hapja e krahëve (cm)",70,270,"0.1")]),
        new("Testing","Testimet fizike",[
            new("Sprint20","Sprint 20 m (sekonda)",1,40,"0.01"),
            new("Sprint10","Sprint 10 m (sekonda)",0.5m,30,"0.01"),new("Sprint30","Sprint 30 m (sekonda)",1,60,"0.01"),
            new("Jump","Kërcim vertikal (cm)",1,150,"0.1"),new("Grip","Forca e kapjes (kg)",1,150,"0.1"),
            new("Run6","Vrapim 6 minuta (m)",50,3000,"1"),
            new("CMJ","Kërcimi CMJ (cm)",1,150,"0.1"),new("YoYoIR1","Yo-Yo IR1 · distanca (m)",40,6000,"40"),
            new("Agility505Left","Testi 505 · kthimi majtas (s)",1,20,"0.01"),new("Agility505Right","Testi 505 · kthimi djathtas (s)",1,20,"0.01"),
            new("Protocol","Protokolli / pajisja e matjes")]),
        new("Talent","Identifikimi i talentit",[
            new("Potential","Potenciali / 5",1,5,Slider:true),new("Learning","Përvetësimi / 5",1,5,Slider:true),
            new("GameReading","Leximi i lojës / 5",1,5,Slider:true),new("Commitment","Përkushtimi / 5",1,5,Slider:true),
            new("Status","Vendimi i trajnerit",Choices:["Në vëzhgim","Për ndjekje të afërt","Për provë në nivel më të lartë"]),
            new("Evidence","Vëzhgimet dhe hapi i ardhshëm")])
    ];
    public static DevelopmentSection? Find(string key) => All.SingleOrDefault(x=>x.Key==key);
    public static Dictionary<string,string> Validate(DevelopmentRecordInput input, DevelopmentSection? configured = null)
    {
        var section=configured??Find(input.Kind)??throw new ValidationException("Pjesa e zgjedhur nuk është e vlefshme.");
        if(input.Values.Any(x=>!string.IsNullOrWhiteSpace(x.Value)&&!section.Fields.Any(f=>f.Key==x.Key)))throw new ValidationException("Fushat kanë ndryshuar. Rihapni formularin.");
        if(input.Date==default || input.Date>BillingClock.Today)throw new ValidationException("Zgjidhni datën e sotme ose një datë të kaluar.");
        Validator.ValidateObject(input,new ValidationContext(input),true);
        var result=new Dictionary<string,string>();
        foreach(var field in section.Fields){
            var value=input.Values.GetValueOrDefault(field.Key)?.Trim();
            if(input.Kind=="Profile"&&field.Key=="PlayerRole")value=RoleLabel(value);
            if(string.IsNullOrEmpty(value))continue;
            if(value.Length>2000)throw new ValidationException(field.Label+": teksti është shumë i gjatë.");
            if(field.Choices!=null && !field.Choices.Contains(value))throw new ValidationException(field.Label+": zgjidhni një vlerë nga lista.");
            if(field.Min.HasValue){
                if(!decimal.TryParse(value.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var number) || number<field.Min || number>field.Max || number%decimal.Parse(field.Step,CultureInfo.InvariantCulture)!=0)
                    throw new ValidationException(field.Label+": vlera nuk është e vlefshme.");
                value=number.ToString(CultureInfo.InvariantCulture);
            }
            result[field.Key]=value;
        }
        if(result.Count==0)throw new ValidationException("Plotësoni së paku një fushë.");
        if(input.Kind=="Profile"&&result.TryGetValue("PrimaryPosition",out var primary)&&result.TryGetValue("SecondaryPosition",out var secondary)&&primary==secondary)
            throw new ValidationException("Pozita e dytë duhet të jetë ndryshe nga kryesorja.");
        if(input.Kind=="Match")foreach(var pair in new[]{("OnTarget","Shots"),("Completed","Passes"),("DuelsWon","Duels")}){
            if(result.TryGetValue(pair.Item1,out var part)&&result.TryGetValue(pair.Item2,out var total)&&decimal.Parse(part,CultureInfo.InvariantCulture)>decimal.Parse(total,CultureInfo.InvariantCulture))
                throw new ValidationException("Rezultati i suksesshëm nuk mund të jetë më i madh se numri i tentimeve.");
        }
        return result;
    }
}
