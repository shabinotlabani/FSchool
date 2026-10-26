using System.Net;
using System.Text.RegularExpressions;
using _2Korriku.Controllers;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static class HttpChecks
{
    public static async Task Run(string root, string connection, bool preview)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(ClubsController).Assembly.GetName().Name, ContentRootPath = root, EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddScoped<ClubContext>();
        builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connection));
        builder.Services.AddDefaultIdentity<ApplicationUser>(o => { o.Password.RequireNonAlphanumeric = false; o.User.RequireUniqueEmail = true; }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
        builder.Services.AddAuthorization(o => o.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Roles.All).Build());
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(ClubsController).Assembly);
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<IAntiforgeryAdditionalDataProvider, ClubAntiforgeryData>();
        builder.Services.AddScoped<ClubBranding>(); builder.Services.AddScoped<UserAdministrationService>();
        builder.Services.AddScoped<DashboardService>(); builder.Services.AddScoped<FinancialService>();
        builder.Services.AddScoped<BillingService>(); builder.Services.AddScoped<TariffService>();
        await using var app = builder.Build();
        app.Use(async (context,next)=>{try{await next();}catch(Exception e){Console.WriteLine("HTTP render failure: "+e);throw;}});
        app.UseStaticFiles(); app.UseRouting();
        app.Use(async (http, next) => { http.RequestServices.GetRequiredService<ClubContext>().IdentityAccess = true; await next(); });
        app.UseAuthentication(); app.UseMiddleware<ClubMiddleware>(); app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"); app.MapRazorPages();
        var password = "TestA9" + Guid.NewGuid().ToString("N");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); db.Club.ClubId = 2; db.Club.ManagePlatform = true; db.Club.IdentityAccess = true;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = await users.FindByEmailAsync("labinot.shabani@gmail.com") ?? throw new Exception("Owner missing");
            owner.PasswordHash = users.PasswordHasher.HashPassword(owner, password); await users.UpdateAsync(owner);
            db.Club.ManagePlatform = false;
            var staff = new ApplicationUser { Email = "http-admin@example.test", UserName = "http-admin@example.test", EmailConfirmed = true };
            var created = await users.CreateAsync(staff, password);
            if (!created.Succeeded) throw new Exception("Test user setup failed: " + string.Join(",", created.Errors.Select(x => x.Code)));
            await users.AddToRoleAsync(staff, Roles.Admin);
        }
        await app.StartAsync();
        try
        {
            var url = app.Urls.Single();
            HttpClient Client() => new(new HttpClientHandler { CookieContainer = new(), AllowAutoRedirect = false }) { BaseAddress = new(url) };
            static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            static void Check(bool ok, string text) { if (!ok) throw new Exception(text); Console.WriteLine("PASS: " + text); }
            using(var languageClient=Client()) {
                foreach(var language in LoginLanguage.Codes) {
                    var response=await languageClient.GetAsync("/Identity/Account/Login?language="+language+"&returnUrl=%2FCoaches%2FPlayers");
                    var html=await response.Content.ReadAsStringAsync();
                    Check(response.IsSuccessStatusCode&&WebUtility.HtmlDecode(html).Contains("<h1>"+LoginLanguage.Text(language,"Title")+"</h1>"),"Login renders language "+language);
                    var repeat=WebUtility.HtmlDecode(await languageClient.GetStringAsync("/Identity/Account/Login"));
                    Check(repeat.Contains("<h1>"+LoginLanguage.Text(language,"Title")+"</h1>"),"Login language cookie persists "+language);
                    var invalid=await languageClient.PostAsync("/Identity/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=Token(html),["language"]=language,["Input.Email"]="",["Input.Password"]=""}));
                    Check(WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync()).Contains(LoginLanguage.Text(language,"EmailRequired")),"Login validation translated "+language);
                }
                var fallback=await languageClient.GetStringAsync("/Identity/Account/Login?language=invalid");
                Check(fallback.Contains("lang=\"sq\""),"Unsupported login language falls back to Albanian");
            }
            async Task<HttpResponseMessage> Post(HttpClient client, string action, string form, Dictionary<string, string> values)
            {
                var response = await client.GetAsync(form); Check(response.StatusCode == HttpStatusCode.OK, "Form renders: " + form);
                values["__RequestVerificationToken"] = Token(await response.Content.ReadAsStringAsync());
                Check(values["__RequestVerificationToken"].Length > 0, "Antiforgery token rendered");
                return await client.PostAsync(action, new FormUrlEncodedContent(values));
            }
            using var ownerClient = Client();
            var login = await Post(ownerClient, "/Identity/Account/Login", "/Identity/Account/Login", new() { ["Input.Email"] = "labinot.shabani@gmail.com", ["Input.Password"] = password });
            Check(login.StatusCode == HttpStatusCode.Redirect && login.Headers.Location?.OriginalString == "/Clubs", "Real owner login opens club selection");
            var page = await ownerClient.GetStringAsync("/Clubs"); Check(page.Contains("Te cili klub do të futesh?"), "Club selector view renders");
            var create = await Post(ownerClient, "/Clubs/Save", "/Clubs/Edit", new() { ["Name"] = "HTTP test club", ["Sport"]="Basketball", ["ExpiresOn"] = BillingClock.Today.AddYears(1).ToString("yyyy-MM-dd"), ["IsActive"] = "true", ["Id"] = "0" });
            Check(create.StatusCode == HttpStatusCode.Redirect, "Owner creates club through actual MVC form");
            var selected = await Post(ownerClient, "/Clubs/Select", "/Clubs", new() { ["id"] = "3" });
            Check(selected.StatusCode == HttpStatusCode.Redirect, "Owner selects newly created club");
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); db.Club.ClubId = 2;
                var otherReceipt = await db.Payments.Select(x => x.Id).SingleAsync();
                Check((await ownerClient.GetAsync("/Payments/Receipt/" + otherReceipt)).StatusCode == HttpStatusCode.NotFound, "Forged receipt URL cannot reveal another club's payment");
            }
            var studentForm = await ownerClient.GetAsync("/Students/Create");
            Check(studentForm.StatusCode == HttpStatusCode.OK, "Player registration renders in new club");
            var usersForm = await ownerClient.GetStringAsync("/Users/Create");
            var oldToken = Token(usersForm);
            var userSave = await ownerClient.PostAsync("/Users/Save", new FormUrlEncodedContent(new Dictionary<string, string> {
                ["__RequestVerificationToken"] = oldToken, ["FirstName"] = "Club", ["LastName"] = "Admin", ["Email"] = "new-club-admin@example.test",
                ["Role"] = Roles.Admin, ["Password"] = password, ["ConfirmPassword"] = password, ["IsActive"] = "true"
            }));
            Check(userSave.StatusCode == HttpStatusCode.Redirect, "Owner creates club administrator");
            var duplicateEmail = await ownerClient.PostAsync("/Users/Save", new FormUrlEncodedContent(new Dictionary<string, string> {
                ["__RequestVerificationToken"] = oldToken, ["FirstName"] = "Duplicate", ["LastName"] = "Email", ["Email"] = "http-admin@example.test",
                ["Role"] = Roles.Admin, ["Password"] = password, ["ConfirmPassword"] = password, ["IsActive"] = "true"
            }));
            Check(duplicateEmail.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await duplicateEmail.Content.ReadAsStringAsync()).Contains("Ky email është përdorur"), "Duplicate email in another club returns validation instead of creating a second membership");
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Check(await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == "new-club-admin@example.test" && x.ClubId == 3 && !x.IsPlatformOwner), "New administrator receives club ID server-side");
            }
            await Post(ownerClient, "/Clubs/Select", "/Clubs", new() { ["id"] = "1" });
            var stale = await ownerClient.PostAsync("/Users/Save", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = oldToken }));
            Check(stale.StatusCode == HttpStatusCode.BadRequest, "HTTP rejects form from previously selected club");
            using var staffClient = Client();
            var staffLogin = await Post(staffClient, "/Identity/Account/Login", "/Identity/Account/Login", new() { ["Input.Email"] = "new-club-admin@example.test", ["Input.Password"] = password });
            Check(staffLogin.StatusCode == HttpStatusCode.Redirect && staffLogin.Headers.Location?.OriginalString != "/Clubs", "Ordinary staff enter their own club directly");
            Check((await staffClient.GetAsync("/Clubs")).StatusCode != HttpStatusCode.OK, "Club administrator cannot manage platform clubs");
            using var expiredClient = Client();
            await Post(expiredClient, "/Identity/Account/Login", "/Identity/Account/Login", new() { ["Input.Email"] = "http-admin@example.test", ["Input.Password"] = password });
            Check((await expiredClient.GetAsync("/Students")).Headers.Location?.OriginalString == "/Clubs/Expired", "Real expired account is blocked");
            int chatStudent; string chatCoach;
            var parentToken=new string('f',64);
            using(var scope=app.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();db.Club.ClubId=3;
                var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var coach=new ApplicationUser{UserName="chat-http@example.test",Email="chat-http@example.test",FirstName="Arben",LastName="Trajneri",EmailConfirmed=true};
                Check((await users.CreateAsync(coach,password)).Succeeded,"Chat coach fixture created");await users.AddToRoleAsync(coach,Roles.Coach);chatCoach=coach.Id;
                var team=new TrainingTeam{Name="U12",CoachId=coach.Id,MinAge=0,MaxAge=99,Capacity=30};
                var student=new Student{FirstName="Luan",LastName="Krasniqi",ParentName="Prindi i Luanit",DateOfBirth=new(2015,1,1),TrainingTeam=team,ParentLinkToken=parentToken};
                db.Students.Add(student);
                db.Students.Add(new Student{FirstName="Ardit",LastName="Hoxha",DateOfBirth=new(2015,1,1),TrainingTeam=team});
                await db.SaveChangesAsync();chatStudent=student.Id;
            }
            using var parentClient=Client();using var coachClient=Client();
            var thread=$"/p/{parentToken}/chat?studentId={chatStudent}";
            var parentPage=await parentClient.GetAsync(thread);
            Check(parentPage.StatusCode==HttpStatusCode.OK,"Parent chat page renders with private link");
            Check((await parentClient.GetAsync($"/p/{parentToken}/chat?studentId=2147483647")).StatusCode==HttpStatusCode.NotFound,"Parent chat rejects forged player ID");
            Check((await parentClient.PostAsync($"/p/{parentToken}/chat/send",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Chat send requires antiforgery token");
            var parentSend=await Post(parentClient,$"/p/{parentToken}/chat/send",thread,new(){["StudentId"]=chatStudent.ToString(),["CoachId"]=chatCoach,["Body"]="Pershendetje, a mund te flasim per stervitjen? <script>alert(1)</script>",["RequestId"]=Guid.NewGuid().ToString()});
            Check(parentSend.StatusCode==HttpStatusCode.Redirect,"Parent sends message through actual form");
            await Post(coachClient,"/Identity/Account/Login","/Identity/Account/Login",new(){["Input.Email"]="chat-http@example.test",["Input.Password"]=password});
            var matchForm=await coachClient.GetStringAsync("/Matches/Edit");
            var matchTeam=Regex.Match(matchForm,"name=\"TeamId\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Check(matchTeam.Length>0&&matchForm.Contains("match-form")&&!matchForm.Contains("match-lineup")&&!matchForm.Contains("name=\"ScoreFor\""),"New match form contains metadata only");
            var matchSave=await Post(coachClient,"/Matches/Edit","/Matches/Edit",new(){["TeamId"]=matchTeam,["RequestId"]=Guid.NewGuid().ToString(),["Opponent"]="Visitors U12",["Kind"]="Friendly",["Status"]="Completed",["Date"]=BillingClock.Today.ToString("yyyy-MM-dd"),["Duration"]="40",["PlayersOnCourt"]="5",["ScoreFor"]="20",["ScoreAgainst"]="18",["Players[0].StudentId"]=chatStudent.ToString(),["Players[0].Role"]="Starter",["Players[0].Intervals[0].From"]="0",["Players[0].Intervals[0].To"]="40"});
            Check(matchSave.StatusCode==HttpStatusCode.Redirect,"Coach creates match before adding result and lineup");
            var matchLocation=matchSave.Headers.Location!.OriginalString;
            var savedMatchForm=await coachClient.GetStringAsync(matchLocation);
            Check(savedMatchForm.Contains("match-lineup")&&savedMatchForm.Contains("name=\"ScoreFor\""),"Created match opens result and minutes editor");
            Check(!(await coachClient.GetStringAsync("/Matches/Report")).Contains("Luan"),"Creation does not record submitted scores or minutes");
            string MatchHidden(string name)=>Regex.Match(savedMatchForm,"name=\""+name+"\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            var completedMatch=await Post(coachClient,"/Matches/Edit",matchLocation,new(){["Id"]=MatchHidden("Id"),["Revision"]=MatchHidden("Revision"),["RequestId"]=MatchHidden("RequestId"),["TeamId"]=matchTeam,["Opponent"]="Visitors U12",["Kind"]="Friendly",["Status"]="Completed",["Date"]=BillingClock.Today.ToString("yyyy-MM-dd"),["Duration"]="40",["PlayersOnCourt"]="5",["ScoreFor"]="20",["ScoreAgainst"]="18",["Players[0].StudentId"]=chatStudent.ToString(),["Players[0].Role"]="Starter",["Players[0].Intervals[0].From"]="0",["Players[0].Intervals[0].To"]="40"});
            Check(completedMatch.StatusCode==HttpStatusCode.Redirect,"Coach adds result and playing time in second step");
            Check((await coachClient.GetStringAsync("/Matches/Report")).Contains("Luan"),"Minute report renders recorded player");
            using(var scope=app.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();db.Club.ClubId=3;
                var weekStart=BillingClock.Today.AddDays(-(((int)BillingClock.Today.DayOfWeek+6)%7));
                foreach(var fixture in new[]{(Day:weekStart.AddDays(-1),Name:"Outside previous week"),(Day:weekStart.AddDays(7),Name:"Outside next week"),(Day:weekStart,Name:"Monday fixture"),(Day:weekStart.AddDays(6),Name:"Sunday fixture")})
                    db.MatchGames.Add(new MatchGame{TrainingTeamId=int.Parse(matchTeam),Opponent=fixture.Name,Status="Draft",Date=fixture.Day,Kickoff=new TimeOnly(17,30),UpdatedById=chatCoach});
                await db.SaveChangesAsync();
            }
            var adminDashboard=await staffClient.GetStringAsync("/Home/Index");
            Check(adminDashboard.Contains("club-matches")&&adminDashboard.Contains("Visitors U12")&&!adminDashboard.Contains("daily-strip"),"Administrator lands on weekly match agenda");
            Check(adminDashboard.Contains("Monday fixture")&&adminDashboard.Contains("Sunday fixture")&&!adminDashboard.Contains("Outside previous week")&&!adminDashboard.Contains("Outside next week"),"Weekly dashboard includes Monday and Sunday and excludes adjacent weeks");
            var reportUrl="/Home/MatchReport/"+MatchHidden("Id");
            var adminMatch=await staffClient.GetStringAsync(reportUrl);
            Check(adminMatch.Contains("Luan")&&!adminMatch.Contains("Korrigjo"),"Administrator can read match report without coach editing controls");
            Check((await ownerClient.GetAsync(reportUrl)).StatusCode==HttpStatusCode.NotFound,"Match report cannot expose another selected club");
            Check((await coachClient.GetAsync(reportUrl)).StatusCode!=HttpStatusCode.OK,"Coach cannot bypass team assignment through admin report");
            var matrix=await coachClient.GetStringAsync("/Matches/Matrix?teamId="+matchTeam);
            Check(matrix.Contains("Luan")&&matrix.Contains("Ardit")&&matrix.Contains("Visitors U12")&&matrix.Contains("matrix-total"),"Team matrix includes match columns, totals and active players without minutes");
            var progressMinutes=await coachClient.GetStringAsync($"/PlayerDevelopment/Player?id={chatStudent}&section=Progress");
            Check(progressMinutes.Contains("data-match-average>40 min")&&progressMinutes.Contains("player-match-summary"),"Player progress renders match average");
            Check((await coachClient.GetAsync("/Matches/Matrix?teamId=2147483647")).StatusCode==HttpStatusCode.NotFound,"Matrix rejects unassigned team ID");
            Check((await staffClient.GetAsync("/Matches")).StatusCode!=HttpStatusCode.OK,"Non-coach cannot enter match management");
            Check((await parentClient.PostAsync("/Matches/Edit",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode!=HttpStatusCode.OK,"Anonymous parent cannot save match report");
            Check((await coachClient.GetStringAsync("/CoachInbox")).Contains("Luan"),"Coach inbox displays parent conversation");
            var coachThread=$"/CoachInbox/Thread?studentId={chatStudent}";
            var coachHtml=await coachClient.GetStringAsync(coachThread);
            Check(coachHtml.Contains("&lt;script&gt;")&&!coachHtml.Contains("<script>alert(1)</script>"),"Chat message text is HTML-escaped");
            var reply=await Post(coachClient,"/CoachInbox/Send",coachThread,new(){["StudentId"]=chatStudent.ToString(),["Body"]="Pershendetje! Po, shihemi para stervitjes se neserme.",["RequestId"]=Guid.NewGuid().ToString()});
            Check(reply.StatusCode==HttpStatusCode.Redirect,"Coach replies through inbox");
            Check((await parentClient.GetStringAsync($"/p/{parentToken}/chat/messages?studentId={chatStudent}&coachId={chatCoach}")).Contains("neserme"),"Parent polling endpoint receives coach reply");
            Check((await coachClient.GetAsync($"/CoachInbox/Deactivate?studentId={chatStudent}")).StatusCode==HttpStatusCode.OK,"Coach deactivation confirmation renders");
            var skillsPage=await coachClient.GetStringAsync($"/PlayerDevelopment/Record?studentId={chatStudent}&kind=Skills");
            Check(skillsPage.Contains("field-Shooting")&&!skillsPage.Contains("field-Crossing"),"Basketball assessment form renders sport-specific skills");
            var profilePage=await coachClient.GetStringAsync($"/PlayerDevelopment/Player?id={chatStudent}");
            Check(!profilePage.Contains(">Portier</option>")&&profilePage.Contains("(PG)"),"Basketball player profile shows basketball positions without goalkeeper");
            Check((await staffClient.GetAsync("/DevelopmentSettings")).StatusCode==HttpStatusCode.OK,"Club admin opens metric management grid");
            Check((await coachClient.GetAsync("/DevelopmentSettings")).StatusCode!=HttpStatusCode.OK,"Coach cannot manage club metric definitions");
            var customSave=await Post(staffClient,"/DevelopmentSettings/Edit","/DevelopmentSettings/Edit?section=Measurements",new(){["Sport"]="Basketball",["Section"]="Measurements",["Label"]="Standing reach (cm)",["Type"]="Number",["Min"]="50",["Max"]="350",["Step"]="0.1",["Enabled"]="true",["SortOrder"]="5"});
            Check(customSave.StatusCode==HttpStatusCode.Redirect,"Admin adds custom measurement through actual form");
            if (preview)
            {
                int footballId;
                using(var scope=app.Services.CreateScope()){
                    var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();db.Club.ClubId=3;
                    foreach(var name in new[]{"Arben Gashi","Erion Berisha","Dion Hoxha","Drin Morina","Altin Shala","Endrit Rama","Aron Hasani","Besart Dema","Rion Kelmendi","Ledion Krasniqi","Ermal Berisha","Arian Shala"}){
                        var parts=name.Split(' ');db.Students.Add(new Student{FirstName=parts[0],LastName=parts[1],DateOfBirth=new(2015,1,1),TrainingTeamId=int.Parse(matchTeam)});
                    }
                    var game=new MatchGame{TrainingTeamId=int.Parse(matchTeam),Sport="Football",Opponent="Football visitors",Kind="Official",Status="Draft",Date=BillingClock.Today,Duration=90,PlayersOnCourt=11,UpdatedById=chatCoach};
                    game.Players.Add(new(){StudentId=chatStudent,Name="Luan Krasniqi",Role="Starter",Minutes=90,Intervals="[{\"From\":0,\"To\":90}]"});
                    db.MatchGames.Add(game);await db.SaveChangesAsync();footballId=game.Id;
                }
                var runtime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache/codex-runtimes/codex-primary-runtime/dependencies/node");
                var start = new System.Diagnostics.ProcessStartInfo(Path.Combine(runtime, "bin/node.exe")) { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(Path.Combine(root, "tests/MultiClub.Checks/preview.mjs"));
                start.Environment["CLUB_TEST_URL"] = url; start.Environment["CLUB_TEST_PASSWORD"] = password;
                start.Environment["CHAT_TEST_STUDENT"]=chatStudent.ToString();start.Environment["CHAT_TEST_TOKEN"]=parentToken;
                start.Environment["MATCH_TEST_LOCATION"]=matchLocation;
                start.Environment["FOOTBALL_TEST_LOCATION"]="/Matches/Edit/"+footballId;
                start.Environment["CLUB_TEST_PLAYWRIGHT"] = Path.Combine(runtime, "node_modules/playwright/index.mjs");
                using var process = System.Diagnostics.Process.Start(start)!;
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                Console.WriteLine(await stdout);
                if(process.ExitCode != 0) Console.WriteLine(await stderr);
                Check(process.ExitCode == 0, "Desktop and mobile browser previews completed");
            }
        }
        finally { await app.StopAsync(); }
    }
}
