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
var database="student_list_test_"+Guid.NewGuid().ToString("N");
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

    await using(var scope=provider.CreateAsyncScope()){
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var date=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        db.Students.AddRange(Enumerable.Range(1,5000).Select(i=>new Student {
            FirstName="LoadTest",LastName=i.ToString("D4"),DateOfBirth=new(2015,1,1),
            CreatedAt=date.AddMinutes(i), ParentName="Test Parent",ParentPhone=i==1?"044999888":null
        }));
        await db.SaveChangesAsync();
        var controller=new _2Korriku.Controllers.StudentsController(db);
        async Task<List<_2Korriku.Controllers.StudentListItem>> List(string? search,int page=1){
            var result=(Microsoft.AspNetCore.Mvc.ViewResult)await controller.Index(search,page);
            return (List<_2Korriku.Controllers.StudentListItem>)result.Model!;
        }
        var expected=await db.Students.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Take(10).Select(x=>x.Id).ToListAsync();
        var first=await List(null);
        Check(first.Count==10&&first.Select(x=>x.Id).SequenceEqual(expected),"5001 players: initial page loads only latest ten in stable order");
        var second=await List(null,2);
        Check(second.Count==10&&!second.Any(x=>first.Any(y=>y.Id==x.Id)),"Next page has ten distinct players");
        Check((await List("LoadTest")).Count==10&&(int)controller.ViewData["Total"]! ==5000,"Broad search is limited to ten with accurate total");
        Check((await List("LoadTest 0001")).Single().LastName=="0001","Search finds older player outside loaded page");
        Check((await List("044999888")).Single().LastName=="0001","Phone search finds older player");
        Check((await List("%_")).Count==0,"Search wildcards remain literal");
        Check((await List(null,int.MaxValue)).Count==1&&(int)controller.ViewData["Page"]! ==501,"Out-of-range page clamps safely");
        Check((await List(null,-1)).Count==10&&(int)controller.ViewData["Page"]! ==1,"Negative page clamps to first page");
        db.Clubs.Add(new ClubTenant{Id=2,Name="Other Club"});await db.SaveChangesAsync();db.Club.ClubId=2;
        Check((await List("LoadTest")).Count==0,"Search stays within selected club");
    }
    using var staff=Client();await Login(staff,"admin");
    var html=await staff.GetStringAsync("/Students?search=LoadTest&page=2");
    Check(Regex.Matches(html,"class=\"player-cell\"").Count==10,"Rendered table contains only ten rows");
    Check(html.Contains("search=LoadTest")&&html.Contains("page=3"),"Next-page links retain search");
}

finally{
    if(server is {HasExited:false}){server.Kill(true);await server.WaitForExitAsync();}server?.Dispose();
    await using var drop=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();Console.WriteLine("Temporary test database removed.");
}
