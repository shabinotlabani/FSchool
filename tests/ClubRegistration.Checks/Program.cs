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
var database="registration_test_"+Guid.NewGuid().ToString("N");
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

    var form = await anon.GetStringAsync("/Clubs/Register");
    Check((await anon.GetStringAsync("/Identity/Account/Login")).Contains("/Clubs/Register"), "Login links to registration");
    var data = new Dictionary<string,string> {
        ["Name"] = "  Test Club <script>  ", ["Activity"] = "Swimming", ["Email"] = "club@example.test",
        ["Phone"] = "+383 44 123 456", ["ReviewedAt"] = "2026-01-01", ["IsActive"] = "true",
        ["__RequestVerificationToken"] = Token(form)
    };
    Check((await anon.PostAsync("/Clubs/Register",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode == HttpStatusCode.BadRequest, "Antiforgery required");
    data["Email"] = "invalid";
    Check((await anon.PostAsync("/Clubs/Register",new FormUrlEncodedContent(data))).StatusCode == HttpStatusCode.OK, "Invalid email returns form");
    data["Email"] = "club@example.test";
    var sent = await anon.PostAsync("/Clubs/Register",new FormUrlEncodedContent(data));
    Check(sent.StatusCode == HttpStatusCode.Redirect && sent.Headers.Location?.ToString() == "/Clubs/RegistrationThanks", "Valid request redirects to thank-you page");
    Check((await anon.GetAsync("/Clubs/RegistrationThanks")).StatusCode == HttpStatusCode.OK, "Thank-you page is public");
    int requestId;
    await using(var scope = provider.CreateAsyncScope()) {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var request = await db.ClubRegistrations.SingleAsync(); requestId = request.Id;
        Check(request.ReviewedAt == null && request.Name == "Test Club <script>", "Request stored pending and trimmed; posted review status ignored");
        Check(await db.Clubs.CountAsync() == 1 && await db.Users.CountAsync() == 4, "Submission does not create club or account");
        Check(!db.Database.HasPendingModelChanges(), "Migration matches the model");
        db.Club.IsOwner = false;
        Check(await db.ClubRegistrations.CountAsync() == 0, "Non-owner database queries cannot read requests");
        request.ReviewedAt = DateTime.UtcNow;
        var blocked = false; try { await db.SaveChangesAsync(); } catch(InvalidOperationException) { blocked = true; }
        Check(blocked, "Database guard rejects non-owner review");
    }
    Check((await anon.GetAsync("/Clubs/Requests")).StatusCode == HttpStatusCode.Redirect, "Anonymous inbox denied");
    using var staff = Client(); await Login(staff,"admin");
    var denied = await staff.GetAsync("/Clubs/Requests");
    Check(denied.StatusCode == HttpStatusCode.Forbidden || denied.Headers.Location?.ToString().Contains("AccessDenied") == true, "Club administrator cannot read platform requests");
    using var owner = Client(); await Login(owner,"owner");
    var inbox = await owner.GetStringAsync("/Clubs/Requests");
    Check(inbox.Contains("club@example.test") && !inbox.Contains("Test Club <script>"), "Owner sees requests with HTML encoding");
    Check((await owner.PostAsync("/Clubs/ReviewRequest",new FormUrlEncodedContent(new Dictionary<string,string>{{"id",requestId.ToString()},{"__RequestVerificationToken",Token(inbox)}}))).StatusCode == HttpStatusCode.Redirect, "Owner can mark request reviewed");
    await using(var scope = provider.CreateAsyncScope()) {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Check((await db.ClubRegistrations.SingleAsync()).ReviewedAt != null, "Reviewed status persisted");
        Check(await db.Clubs.CountAsync() == 1 && await db.Users.CountAsync() == 4, "Review does not provision club or account");
    }
    if(args.Length>1){
        var browser=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true};browser.ArgumentList.Add(Path.GetFullPath(args[1]));browser.ArgumentList.Add(url);
        using var check=Process.Start(browser)!;await check.WaitForExitAsync();Check(check.ExitCode==0,"Responsive browser checks");
    }
}

finally{
    if(server is {HasExited:false}){server.Kill(true);await server.WaitForExitAsync();}server?.Dispose();
    await using var drop=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();Console.WriteLine("Temporary test database removed.");
}
