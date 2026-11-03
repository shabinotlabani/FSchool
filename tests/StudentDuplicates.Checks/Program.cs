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
var source=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new Exception("Set a test PostgreSQL connection in ConnectionStrings__DefaultConnection."));
var database="duplicates_test_"+Guid.NewGuid().ToString("N");
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

    int teamId;
    await using(var scope=provider.CreateAsyncScope()) {
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        teamId=await db.TrainingTeams.Select(x=>x.Id).FirstAsync();
        var existing=await db.Students.SingleAsync();
        existing.ParentPhone="+383 44 123 456"; existing.ParentName="Shared Parent"; existing.IsActive=false;
        db.Clubs.Add(new ClubTenant{Id=2,Name="Other club"}); await db.SaveChangesAsync();
        db.Club.ClubId=2;
        db.Students.Add(new(){FirstName="Secret",LastName="Player",ParentPhone="044123456",DateOfBirth=new(2015,1,1)});await db.SaveChangesAsync();
        db.Club.ClubId=1;
        var service=new StudentDuplicateService(db);
        var names=await service.FindAsync([new(" manual ","PLAYER",null,null)]);
        Check(names.Matches.Count==1&&names.Matches[0].NameMatches&&!names.Matches[0].IsActive,"Case/space normalized names include inactive players");
        foreach(var phone in new[]{"044123456","0038344123456","+383 (44) 123-456"}){
            var found=await service.FindAsync([new("Other","Person",phone,null)]);
            Check(found.Matches.Count==1&&found.Matches[0].PhoneMatches,"Phone variants match parent phone, without cross-club data: "+phone);
        }
        Check((await service.FindAsync([new("Manual","Different",null,null)])).Matches.Count==0,"First name alone is not a duplicate");
        Check((await service.FindAsync([new(null,null,null,null)])).Matches.Count==0,"Blank values never match");
        Check((await service.FindAsync([new(null,null,"+35544123456",null)])).Matches.Count==0,"Different country code does not match");
        Check(!db.Database.HasPendingModelChanges(),"No migration required for duplicate warnings");
        var family=new FamilyRegistrationModel{FamilyName="Duplicate test family",ParentName="Shared Parent",ParentPhone="044123456",FeePlanId=2,
            Children=[new(){FirstName="Sibling",LastName="One",DateOfBirth=new(2015,1,1),TrainingTeamId=teamId},new(){FirstName="Sibling",LastName="Two",DateOfBirth=new(2016,1,1),TrainingTeamId=teamId}]};
        StudentDuplicateReview? review=null;
        try{await new StudentWorkflowService(db).RegisterFamilyAsync(family,"admin");}catch(StudentDuplicateWarning ex){review=ex.Review;}
        Check(review?.Matches.Count==1&&!await db.PlayerFamilies.AnyAsync(),"Family registration warns before creating family or children");
        family.ConfirmDuplicate=true;family.DuplicateToken=review!.Token;
        var familyId=await new StudentWorkflowService(db).RegisterFamilyAsync(family,"admin");
        Check(familyId>0&&await db.Students.CountAsync()==3,"Distinct siblings with shared phone can be registered after confirmation");
        Check(await new StudentWorkflowService(db).RegisterFamilyAsync(family,"admin")==familyId,"Family resubmission retains idempotency");
    }
    using var staff=Client();await Login(staff,"admin");
    var form=await staff.GetStringAsync("/Students/Create");
    var data=new Dictionary<string,string>{{"FirstName","New"},{"LastName","Sibling"},{"DateOfBirth","2015-01-01"},{"Phone","044123456"},
        {"TrainingTeamId",teamId.ToString()},{"IsContractPlayer","true"},{"__RequestVerificationToken",Token(form)}};
    var preview=await staff.PostAsync("/Students/CheckDuplicates",new FormUrlEncodedContent(data));
    var previewHtml=await preview.Content.ReadAsStringAsync();
    Check(preview.StatusCode==HttpStatusCode.OK&&previewHtml.Contains("Shared Parent")&&previewHtml.Contains("01/01/2015")&&!previewHtml.Contains("Secret"),"Live preview shows contact and birth details only from current club");
    Check(preview.Headers.CacheControl?.NoStore==true,"Duplicate details are not cached");
    Check((await staff.PostAsync("/Students/CheckDuplicates",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Live lookup requires antiforgery");
    var warning=await staff.PostAsync("/Students/Create",new FormUrlEncodedContent(data));
    var warningHtml=await warning.Content.ReadAsStringAsync();
    Check(warning.StatusCode==HttpStatusCode.OK&&warningHtml.Contains("name=\"ConfirmDuplicate\""),"Submit without JavaScript returns review instead of saving");
    await using(var scope=provider.CreateAsyncScope())Check(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Students.CountAsync()==3,"Warning does not save player");
    var duplicateToken=WebUtility.HtmlDecode(Regex.Match(warningHtml,"name=\"DuplicateToken\" value=\"([^\"]+)\"").Groups[1].Value);
    data["ConfirmDuplicate"]="true";data["DuplicateToken"]=duplicateToken;data["FirstName"]="Changed";
    var stale=await staff.PostAsync("/Students/Create",new FormUrlEncodedContent(data));
    Check(stale.StatusCode==HttpStatusCode.OK,"Changed identity invalidates old acknowledgement");
    var refreshed=await stale.Content.ReadAsStringAsync();
    data["DuplicateToken"]=WebUtility.HtmlDecode(Regex.Match(refreshed,"name=\"DuplicateToken\" value=\"([^\"]+)\"").Groups[1].Value);
    Check((await staff.PostAsync("/Students/Create",new FormUrlEncodedContent(data))).StatusCode==HttpStatusCode.Redirect,"Operator can explicitly continue for another person");
    await using(var scope=provider.CreateAsyncScope())Check(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Students.CountAsync()==4,"Confirmed registration saves one player");
    if(args.Length>1){
        var browser=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true};browser.ArgumentList.Add(Path.GetFullPath(args[1]));browser.ArgumentList.Add(url);
        using var check=Process.Start(browser)!;await check.WaitForExitAsync();Check(check.ExitCode==0,"Responsive browser and live lookup checks");
    }
}

finally{
    if(server is {HasExited:false}){server.Kill(true);await server.WaitForExitAsync();}server?.Dispose();
    await using var drop=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();Console.WriteLine("Temporary test database removed.");
}
