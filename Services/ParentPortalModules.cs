using _2Korriku.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public static class ParentPortalModules
{
    public const string All="Attendance,Finance,Events,Progress,Menu,Doctor,Psychologist,Chat,Parking,Notifications,Photo,Equipment";
    public static readonly (string Key,string Label,string Description)[] Catalog=[
        ("Equipment","Rekuizitat","Fotot, porositë, rezervimet dhe dorëzimet e rekuizitave."),
        ("Attendance","Prezenca dhe orari","Orari, stërvitjet dhe përmbledhja e prezencës."),
        ("Finance","Kartela financiare","Gjendja, pagesat dhe historiku financiar."),
        ("Events","Eventet & Lejet","Ftesat dhe autorizimet prindërore."),
        ("Progress","Ecuria dhe vlerësimet","Matjet, aftësitë dhe vlerësimet e lojtarit."),
        ("Menu","Plani ushqimor","Menuja dhe udhëzimet e ushqimit."),
        ("Doctor","Mjeku","Udhëzimet mjekësore të ndara me prindin."),
        ("Psychologist","Psikologu","Moduli i psikologut në portal."),
        ("Chat","Kontakto trajnerin","Mesazhet mes prindit dhe trajnerit."),
        ("Parking","Bileta e parkingut","Bileta në ditët e stërvitjes."),
        ("Notifications","Njoftimet","Aktivizimi dhe dërgimi i njoftimeve push."),
        ("Photo","Fotografia","Ngarkimi dhe ndryshimi i fotos nga prindi.")];
    public static HashSet<string> Parse(string value)=>value.Split(',',StringSplitOptions.RemoveEmptyEntries).Where(x=>Catalog.Any(c=>c.Key==x)).ToHashSet(StringComparer.Ordinal);
    public static async Task<HashSet<string>> Load(ApplicationDbContext db)=>Parse(await db.ClubSettings.AsNoTracking().Select(x=>x.ParentPortalModules).SingleOrDefaultAsync()??All);
    public static async Task<bool> Enabled(ApplicationDbContext db,string key)=>(await Load(db)).Contains(key);
}
public class ParentPortalSettingsInput
{
    public List<string> Enabled { get; set; } = [];
}
[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public sealed class ParentModuleAttribute(string module):Attribute,IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next)
    {
        var db=context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        if(!await ParentPortalModules.Enabled(db,module)){context.Result=new NotFoundResult();return;}
        await next();
    }
}
