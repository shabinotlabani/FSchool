using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS: "+name);}
var root=Path.GetFullPath(args[0]);
var modules=JsonSerializer.Deserialize<List<HelpModule>>(File.ReadAllText(Path.Combine(root,"Content/Help/manual.json")))!;
foreach(var view in Directory.GetFiles(Path.Combine(root,"Views"),"*.cshtml",SearchOption.AllDirectories)){
    var folder=Path.GetFileName(Path.GetDirectoryName(view));var name=Path.GetFileNameWithoutExtension(view);
    if(folder=="Shared"||name.StartsWith('_'))continue;
    Check(modules.Any(m=>m.Key==folder&&m.Pages.ContainsKey(name)),"Coverage: "+folder+"/"+name);
}
var all=modules.SelectMany(m=>m.Pages.Select(p=>new HelpGuide(m.Key+"."+p.Key,m.Title,p.Value,
    m.Details.GetValueOrDefault(p.Key)?.Summary??m.Summary,m.Details.GetValueOrDefault(p.Key)?.Steps??m.Steps,m.Notes))).ToList();
Check(HelpCatalog.Search(all,"si ta korrigjoj borxhin").Any(g=>g.Id=="Payments.CorrectFee"),"Natural-language debt question finds fee correction");
Check(HelpCatalog.Search(all,"fjalekalim").Any(g=>g.Id=="Identity.Login"),"Search ignores Albanian accents");
Check(HelpCatalog.Search(all,"zzznothingzzz").Count==0,"Unmatched question produces no invented answer");
Check(modules.All(m=>m.Steps.Count>=3&&m.Pages.Count>0),"Every module has actionable steps");

var source=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new Exception("Set a test PostgreSQL connection in ConnectionStrings__DefaultConnection."));
var database="manual_test_"+Guid.NewGuid().ToString("N");
await using var admin=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database="postgres",Pooling=false}.ConnectionString);
await admin.OpenAsync();
await using(var create=new NpgsqlCommand($"CREATE DATABASE \"{database}\"",admin))await create.ExecuteNonQueryAsync();
Process? server=null;
try{
    var connection=new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=database,Pooling=false}.ConnectionString;
    var services=new ServiceCollection().AddLogging();
    services.AddScoped(_=>new ClubContext{ClubId=1,IsOwner=true,ManagePlatform=true});
    services.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(connection));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var provider=services.BuildServiceProvider();
    await using(var scope=provider.CreateAsyncScope()){
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();await db.Database.MigrateAsync();
        foreach(var role in Roles.All)db.Roles.Add(new IdentityRole{Id=role,Name=role,NormalizedName=role.ToUpperInvariant()});
        foreach(var (name,role) in new[]{("admin",Roles.Admin),("coach",Roles.Coach),("viewer",Roles.Viewer),("owner",Roles.SuperAdmin)}){
            var user=new ApplicationUser{Id=name,ClubId=1,UserName=name+"@manual.test",NormalizedUserName=(name+"@manual.test").ToUpperInvariant(),Email=name+"@manual.test",NormalizedEmail=(name+"@manual.test").ToUpperInvariant(),EmailConfirmed=true,SecurityStamp=Guid.NewGuid().ToString(),FirstName="Manual",LastName=name,IsPlatformOwner=name=="owner"};
            user.PasswordHash=new PasswordHasher<ApplicationUser>().HashPassword(user,"ManualTest123!");
            db.Users.Add(user);db.UserRoles.Add(new(){UserId=name,RoleId=role});
        }
        var team=new TrainingTeam{Name="Manual team",CoachId="coach",MinAge=0,MaxAge=99,Capacity=30};db.TrainingTeams.Add(team);await db.SaveChangesAsync();
        db.Students.Add(new(){FirstName="Manual",LastName="Player",DateOfBirth=new(2015,1,1),TrainingTeamId=team.Id,ParentLinkToken=new string('a',64)});await db.SaveChangesAsync();
    }
    var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
    var url=$"http://127.0.0.1:{port}";
    var start=new ProcessStartInfo("dotnet"){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
    foreach(var arg in new[]{Path.Combine(root,"bin/Debug/net9.0/2Korriku.dll"),"--urls",url,"--Logging:LogLevel:Default","Warning"})start.ArgumentList.Add(arg);
    start.Environment["ConnectionStrings__DefaultConnection"]=connection;start.Environment["ASPNETCORE_ENVIRONMENT"]="Testing";start.Environment.Remove("PORT");
    server=Process.Start(start)!;
    var logs=new System.Text.StringBuilder();server.OutputDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.AppendLine(e.Data);};server.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)lock(logs)logs.AppendLine(e.Data);};server.BeginOutputReadLine();server.BeginErrorReadLine();
    HttpClient Client()=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new(),UseProxy=false}){BaseAddress=new Uri(url)};
    using var anon=Client();
    for(var i=0;i<100;i++){try{await anon.GetAsync("/health");break;}catch(HttpRequestException){await Task.Delay(200);}}
    if(server.HasExited)throw new Exception("Test server failed: "+logs);
    string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    async Task Login(HttpClient client,string name){var page=await client.GetStringAsync("/Identity/Account/Login");var result=await client.PostAsync("/Identity/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Input.Email",name+"@manual.test"},{"Input.Password","ManualTest123!"},{"__RequestVerificationToken",Token(page)}}));Check(result.StatusCode==HttpStatusCode.Redirect,name+" login");}
    Check((await anon.GetAsync("/Help")).StatusCode==HttpStatusCode.Redirect,"Manual requires authentication");
    Check((await anon.GetStringAsync("/Identity/Account/Login")).Contains("context-help"),"Login has context help");
    using var adminClient=Client();await Login(adminClient,"admin");
    var manual=await adminClient.GetStringAsync("/Help");
    Check(manual.Contains("Payments.CorrectFee")&&manual.Contains("Leagues.Edit"),"Admin sees financial corrections and league guides");
    Check(!manual.Contains("DataReset.Index"),"Non-owner admin does not see destructive owner tools");
    var correction=await adminClient.GetStringAsync("/Help?topic=Payments.CorrectFee");
    Check(WebUtility.HtmlDecode(correction).Contains("përfundimtare, jo diferencën"),"Correction guide explains final amount");
    using var coach=Client();await Login(coach,"coach");var coachManual=await coach.GetStringAsync("/Help");
    Check(coachManual.Contains("Matches.Edit")&&!coachManual.Contains("Payments.CorrectFee")&&!coachManual.Contains("Users.Edit"),"Coach only sees authorized guides");
    Check((await coach.GetAsync("/Help?topic=Payments.CorrectFee")).StatusCode==HttpStatusCode.NotFound,"Direct unauthorized guide is rejected");
    using var viewer=Client();await Login(viewer,"viewer");var viewerManual=await viewer.GetStringAsync("/Help");
    Check(viewerManual.Contains("Payments.Index")&&!viewerManual.Contains("Payments.Register")&&!viewerManual.Contains("Students.Edit"),"Viewer sees read-only instructions");
    var injected=await adminClient.GetStringAsync("/Help?q="+Uri.EscapeDataString("<script>alert(1)</script>"));
    Check(!injected.Contains("<script>alert(1)</script>"),"Search text is HTML encoded");
    Check((await adminClient.GetAsync("/Help?q="+new string('a',201))).StatusCode==HttpStatusCode.BadRequest,"Search length is bounded");
    var portal=await anon.GetStringAsync("/p/"+new string('a',64));Check(portal.Contains("context-help")&&!portal.Contains("Hap manualin dhe"),"Parent portal gets only contextual help");
    if(args.Length>1){
        var browser=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true};browser.ArgumentList.Add(Path.GetFullPath(args[1]));browser.ArgumentList.Add(url);
        using var check=Process.Start(browser)!;await check.WaitForExitAsync();Check(check.ExitCode==0,"Browser interaction and responsive checks");
    }
    Check((await adminClient.GetAsync("/Help")).Headers.CacheControl?.NoStore==true,"Role-specific manual is not cached");
}
finally{
    if(server is {HasExited:false}){server.Kill(true);await server.WaitForExitAsync();}server?.Dispose();
    await using var drop=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();Console.WriteLine("Temporary test database removed.");
}
