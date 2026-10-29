using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;

static class MatchChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        ApplicationDbContext Db(int club=1)=>new(options,new ClubContext{ClubId=club});
        static void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS: "+label);}
        static async Task Reject(Func<Task> action,string label){try{await action();}catch(Exception e)when(e is ValidationException or UnauthorizedAccessException or DbUpdateException){Console.WriteLine("PASS: "+label);return;}throw new Exception("Expected rejection: "+label);}
        int teamId,first,second,bench,foreign,foreignTeamPlayer;
        await using(var db=Db()){
            db.Users.AddRange(new ApplicationUser{Id="match-coach",UserName="match-coach@example.test"},new ApplicationUser{Id="match-other",UserName="match-other@example.test"});
            var team=new TrainingTeam{Name="Match squad",CoachId="match-coach",MinAge=0,MaxAge=99,Capacity=30};
            var other=new TrainingTeam{Name="Other squad",CoachId="match-other",MinAge=0,MaxAge=99,Capacity=30};
            var a=new Student{FirstName="Starter",LastName="Match",DateOfBirth=new(2014,1,1),TrainingTeam=team};
            var b=new Student{FirstName="Substitute",LastName="Match",DateOfBirth=new(2014,1,1),TrainingTeam=team};
            var c=new Student{FirstName="Unused",LastName="Match",DateOfBirth=new(2014,1,1),TrainingTeam=team};
            var d=new Student{FirstName="Other",LastName="Squad",DateOfBirth=new(2014,1,1),TrainingTeam=other};
            db.Students.AddRange(a,b,c,d);await db.SaveChangesAsync();teamId=team.Id;first=a.Id;second=b.Id;bench=c.Id;foreignTeamPlayer=d.Id;
        }
        await using(var db=Db(2)){var s=new Student{FirstName="Foreign",LastName="Match",DateOfBirth=new(2014,1,1)};db.Students.Add(s);await db.SaveChangesAsync();foreign=s.Id;}
        MatchInput Game()=>new(){TeamId=teamId,Opponent="Visitors",Date=BillingClock.Today.AddDays(-1),Duration=60,PlayersOnCourt=1,Status="Completed",ScoreFor=2,ScoreAgainst=1,
            Players=[new(){StudentId=first,Role="Starter",Intervals=[new(){From=0,To=30},new(){From=45,To=60}]},new(){StudentId=second,Role="Substitute",Intervals=[new(){From=30,To=45}]},new(){StudentId=bench,Role="Unused"}]};
        int gameId;
        var placement=Game();placement.Players[0].PitchSlot=1;
        await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(placement)),"Pitch slot must fit match capacity");
        await using(var db=Db()){
            var service=new MatchService(db,false);var input=Game();input.Players[0].CoachRating=4;input.Players[0].CoachNote="  Good positioning  ";gameId=await service.Save("match-coach",input);
            Check(await service.Save("match-coach",input)==gameId&&await db.MatchChanges.CountAsync(x=>x.MatchGameId==gameId)==1,"Repeated match submission is idempotent");
            var rows=await db.MatchPlayers.Where(x=>x.MatchGameId==gameId).ToListAsync();
            Check(rows.Single(x=>x.StudentId==first).Minutes==45&&rows.Single(x=>x.StudentId==second).Minutes==15&&rows.Single(x=>x.StudentId==bench).Minutes==0,"Rolling substitutions calculate minutes without counting bench time");
            var assessment=(await service.Load("match-coach",gameId,teamId)).Players.Single(p=>p.StudentId==first);
            Check(assessment.CoachRating==4&&assessment.CoachNote=="Good positioning","Coach assessment persists independently for each player and match");
            Check(rows.Single(p=>p.StudentId==bench).CoachRating==null,"Unrated players have no invented rating");
            var badRating=Game();badRating.Players[0].CoachRating=6;await Reject(()=>service.Save("match-coach",badRating),"Out-of-range coach rating rejected");
            var longNote=Game();longNote.Players[0].CoachNote=new string('x',1001);await Reject(()=>service.Save("match-coach",longNote),"Oversized coach note rejected");
            var friendly=Game();friendly.Kind="Friendly";await service.Save("match-coach",friendly);
            var draft=Game();draft.Status="Draft";await service.Save("match-coach",draft);
            var cancelled=Game();cancelled.Status="Cancelled";await service.Save("match-coach",cancelled);
            var report=await service.Report("match-coach",teamId,BillingClock.Today.Year,null,null);
            var total=report.Totals.Single(x=>x.StudentId==first);
            Check(total.Games==2&&total.Starts==2&&total.OfficialMinutes==45&&total.FriendlyMinutes==45,"Match reports separate official and friendly minutes, excluding drafts and cancellations");
            Check(report.Totals.Single(x=>x.StudentId==bench).Unused==2&&report.Totals.Single(x=>x.StudentId==bench).Games==0,"Unused substitutes are not counted as appearances");
            Check((await service.Report("match-coach",teamId,BillingClock.Today.Year,"Official",first)).Matches.Count==1,"Player match history filters competition type");
            var cup=Game();cup.Kind="Cup";await service.Save("match-coach",cup);
            var cupReport=await service.Report("match-coach",teamId,BillingClock.Today.Year,"Cup",first);
            Check(cupReport.Matches.Count==1&&cupReport.Totals.Single().CupMinutes==45&&cupReport.Totals.Single().OfficialMinutes==0,"Cup minutes are reported separately from points matches");
            var playing=await service.PlayerMinutes("match-coach",first);
            Check(playing.TotalMinutes==135&&playing.Appearances==3&&playing.Average==45,"Progress average includes completed appearances only");
            var unused=await service.PlayerMinutes("match-coach",bench);
            Check(unused.Appearances==0&&unused.Average==0,"Unused bench does not lower progress average");
            Check((await service.PlayerMinutes("match-coach",first,BillingClock.Today.Year-1)).Appearances==0,"Progress minutes respect selected year");
            Check(new PlayerMatchMinutes(101,3).Average==33.7m,"Per-match average retains one decimal");
            await Reject(()=>service.PlayerMinutes("match-other",first),"Progress minutes reject unassigned coach");
        }
        await using(var db=Db()){
            var service=new MatchService(db,false);
            await Reject(()=>service.Load("match-other",gameId,0),"Other coach cannot open match by ID");
            Check((await service.List("match-other",null,BillingClock.Today.Year,null)).Matches.Count==0,"Coach only lists assigned teams' matches");
            foreach(var id in new[]{foreign,foreignTeamPlayer}){var input=Game();input.Players[1].StudentId=id;await Reject(()=>service.Save("match-coach",input),"Foreign player cannot enter match roster");}
        }
        await using(var db=Db()){
            var service=new MatchService(db,true);
            var draft=Game();draft.Date=BillingClock.Today.AddDays(-100);draft.Status="Draft";draft.Players=[];draft.ScoreFor=null;draft.ScoreAgainst=null;
            var calledMatch=await service.Save("match-coach",draft);
            await service.CallUp("match-coach",calledMatch,foreignTeamPlayer,"Provë me ekipin më të madh");
            var called=await service.Load("match-coach",calledMatch,teamId);
            var player=called.Players.Single(p=>p.StudentId==foreignTeamPlayer);
            Check(player.IsCallUp&&player.HomeTeamName=="Other squad","Match call-up keeps the player's home team");
            player.Role="Starter";player.Intervals=[new(){From=0,To=60}];called.Status="Completed";called.ScoreFor=1;called.ScoreAgainst=0;
            await service.Save("match-coach",called,true);
            var homeReport=await service.Report("match-other",null,BillingClock.Today.Year,null,foreignTeamPlayer);
            Check(homeReport.Totals.Single().OfficialMinutes==60,"Call-up minutes remain visible to the player's home team");
            var secondDraft=Game();secondDraft.Date=draft.Date.AddDays(1);secondDraft.Status="Draft";secondDraft.Players=[];secondDraft.ScoreFor=null;secondDraft.ScoreAgainst=null;
            var secondMatch=await service.Save("match-coach",secondDraft);
            await service.CallUp("match-coach",secondMatch,foreignTeamPlayer,null);
            var secondCalled=await service.Load("match-coach",secondMatch,teamId);var secondPlayer=secondCalled.Players.Single(p=>p.StudentId==foreignTeamPlayer);
            secondPlayer.Role="Starter";secondPlayer.Intervals=[new(){From=0,To=60}];secondCalled.Status="Completed";secondCalled.ScoreFor=1;secondCalled.ScoreAgainst=0;
            await Reject(()=>service.Save("match-coach",secondCalled,true),"Player cannot play two official matches inside 48 hours");
        }
        await using(var db=Db(2)){
            Check(!await db.MatchGames.AnyAsync(),"Match records are isolated by club");
            await Reject(()=>new MatchService(db,false).Load("match-coach",gameId,0),"Other club cannot open match");
        }
        var invalid=Game();invalid.Players[0].Intervals[1].From=20;await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Overlapping player intervals rejected");
        invalid=Game();invalid.Players[1].Intervals[0].From=29;await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Excess players on the field rejected");
        invalid=Game();invalid.Players[0].Intervals[1].To=61;await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Minutes beyond match duration rejected");
        invalid=Game();invalid.Players[0].Intervals[0].From=1;await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Starter must begin at minute zero");
        invalid=Game();invalid.Players[1].Intervals=[];await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Completed substitute requires playing interval");
        invalid=Game();invalid.Date=BillingClock.Today.AddDays(1);await Reject(()=>Task.FromResult(MatchService.ValidateMinutes(invalid)),"Future match cannot be completed");
        MatchInput stale;
        await using(var db=Db()){
            var service=new MatchService(db,false);var setup=Game();setup.Status="Draft";setup.Players[0].Intervals=[new(){From=0,To=60}];setup.Players[1].Role="Unused";setup.Players[1].Intervals=[];
            var stagedId=await service.Save("match-coach",setup);
            var step=await service.Load("match-coach",stagedId,0);step.Status="Completed";
            await Reject(()=>service.Save("match-coach",step),"Football cannot finish before confirming lineup");
            step=await service.Load("match-coach",stagedId,0);step.Status="Live";await service.Save("match-coach",step);
            Check(!(await service.Report("match-coach",teamId,BillingClock.Today.Year,null,null)).Matches.Any(g=>g.Id==stagedId),"In-progress match excluded from final reports");
            step=await service.Load("match-coach",stagedId,0);step.Players.Single(p=>p.StudentId==first).Role="Unused";step.Players.Single(p=>p.StudentId==first).Intervals=[];step.Players.Single(p=>p.StudentId==second).Role="Starter";step.Players.Single(p=>p.StudentId==second).Intervals=[new(){From=0,To=60}];
            await Reject(()=>service.Save("match-coach",step),"Confirmed starting lineup cannot be replaced silently");
            step=await service.Load("match-coach",stagedId,0);step.Status="Completed";await service.Save("match-coach",step);
            Check((await service.Report("match-coach",teamId,BillingClock.Today.Year,null,null)).Matches.Any(g=>g.Id==stagedId),"Finishing confirmed match publishes report totals");
        }
        var football=Game();football.Events=[new(){Kind="Goal",PlayerId=first,Minute=10},new(){Kind="Yellow",PlayerId=bench,Minute=20}];
        FootballService.Validate(football,"Football");Check(true,"Goals and bench cautions accepted");
        football.Events[0].AssistId=first;
        await Reject(()=>Task.Run(()=>FootballService.Validate(football,"Football")),"Scorer cannot assist own goal");
        football.Events[0].AssistId=second;
        await Reject(()=>Task.Run(()=>FootballService.Validate(football,"Football")),"Assist requires player on the field");
        football=Game();football.Events=[new(){Kind="Yellow",PlayerId=bench,Minute=10},new(){Kind="Yellow",PlayerId=bench,Minute=20}];
        FootballService.Validate(football,"Football");Check(football.Events[1].Kind=="SecondYellow","Second caution becomes dismissal");
        football=Game();football.Events=[new(){Kind="Red",PlayerId=first,Minute=10}];
        await Reject(()=>Task.Run(()=>FootballService.Validate(football,"Football")),"Dismissed player cannot keep playing");
        football=Game();football.Players[0].Intervals=[new(){From=0,To=30}];football.Events=[new(){Kind="Red",PlayerId=first,Minute=30}];
        await Reject(()=>Task.Run(()=>FootballService.Validate(football,"Football")),"Dismissed player cannot be replaced");
        await using(var db=Db()){
            var service=new MatchService(db,false);var start=BillingClock.Today.AddDays(-20);
            db.DisciplineRules.Add(new(){TrainingTeamId=teamId,Kind="Official",StartsOn=start,EndsOn=BillingClock.Today.AddDays(20),YellowThreshold=2,YellowBan=1,UpdatedById="match-coach"});await db.SaveChangesAsync();
            foreach(var day in new[]{start,start.AddDays(1),start.AddDays(2)}){var g=Game();g.Date=day;g.Events=[new(){Kind="Yellow",PlayerId=first,Minute=10},new(){Kind="Goal",PlayerId=first,Minute=12}];await service.Save("match-coach",g);}
            var alerts=await new FootballService(db).Alerts(teamId,"Official",start.AddDays(3),null);
            Check(alerts.Any(a=>a.StudentId==first&&a.Remaining==1),"Default yellow threshold alerts for next match");
            Check(!(await new FootballService(db).Alerts(teamId,"Cup",start.AddDays(3),null)).Any(),"Cup does not inherit league cautions");
            var absent=Game();absent.Date=start.AddDays(3);absent.Players.RemoveAt(0);absent.Players[0].Role="Starter";absent.Players[0].Intervals=[new(){From=0,To=60}];await service.Save("match-coach",absent);
            Check(!(await new FootballService(db).Alerts(teamId,"Official",start.AddDays(4),null)).Any(a=>a.StudentId==first&&a.Remaining>0),"Completed match outside squad serves suspension");
            var red=Game();red.Kind="Friendly";red.Date=start;red.Events=[new(){Kind="Red",PlayerId=bench,Minute=10}];await service.Save("match-coach",red);
            Check((await new FootballService(db).Alerts(teamId,"Friendly",start.AddDays(1),null)).Any(a=>a.StudentId==bench&&a.Remaining==1&&!a.NeedsDecision),"Direct red defaults to one suspended match");
            var report=await service.Report("match-coach",teamId,BillingClock.Today.Year,"Official",first);
            Check(report.Totals.Single().Goals==3,"Goal totals aggregate completed match events");
        }
        int leagueId;
        var leagueStart=BillingClock.Today.AddDays(-10);
        await using(var db=Db()){
            await new LeagueService(db).Save(new(){Name="U17 League",StartsOn=leagueStart,EndsOn=BillingClock.Today.AddDays(10),TeamIds=[teamId],RulesConfirmed=true,YellowThreshold=2,YellowBan=1,RedBan=2,SecondYellowBan=1},"match-coach");
            leagueId=await db.Leagues.Where(l=>l.Name=="U17 League").Select(l=>l.Id).SingleAsync();
            var service=new MatchService(db,false);
            foreach(var day in new[]{leagueStart,leagueStart.AddDays(1),leagueStart.AddDays(2)}){var g=Game();g.Date=day;g.LeagueId=leagueId;g.Events=[new(){Kind="Yellow",PlayerId=first,Minute=10}];var id=await service.Save("match-coach",g);Check((await db.MatchGames.SingleAsync(m=>m.Id==id)).LeagueId==leagueId,"Match uses the selected registered league");}
            Check((await new FootballService(db).Alerts(teamId,"Official",leagueStart.AddDays(3),null,0,leagueId)).Any(a=>a.StudentId==first&&a.Remaining==1),"Default rules count only that league's cautions");
            var leagueReport=await service.Report("match-coach",teamId,BillingClock.Today.Year,null,first,leagueId);
            Check(leagueReport.Matches.Count==3&&leagueReport.Totals.Single().OfficialMinutes==135&&leagueReport.Totals.Single().YellowCards==3&&leagueReport.Totals.Single().Goals==0,"Player league filter limits minutes, goals and cards together");
            Check((await service.Report("match-coach",teamId,BillingClock.Today.Year,null,first)).Totals.Single().OfficialMinutes>90,"All leagues restores complete player totals");
            await Reject(()=>service.Report("match-other",null,BillingClock.Today.Year,null,null,leagueId),"League report cannot bypass assigned teams");
            Check((await db.MatchGames.SingleAsync(m=>m.Id==gameId)).LeagueId==null,"Existing matches retain legacy competition history");
            var wrong=Game();wrong.Kind="Cup";wrong.LeagueId=leagueId;
            var derivedId=await service.Save("match-coach",wrong);
            Check((await db.MatchGames.SingleAsync(m=>m.Id==derivedId)).Kind=="Official","Match derives the internal category from the selected league");
            var outside=Game();outside.Date=leagueStart.AddDays(-1);outside.LeagueId=leagueId;
            await Reject(()=>service.Save("match-coach",outside),"Match rejects league outside its season");
            await new LeagueService(db).Save(new(){Name="Overlap",Kind="Cup",StartsOn=leagueStart,EndsOn=BillingClock.Today,TeamIds=[teamId]},"match-coach");
            Check(await db.Leagues.AnyAsync(l=>l.Name=="Overlap"&&l.Kind=="Official"),"League registration does not ask for or trust a cup/points category");
            await new LeagueService(db).Save(new(){Name="Next season",StartsOn=BillingClock.Today.AddDays(11),EndsOn=BillingClock.Today.AddDays(100),TeamIds=[teamId],RulesConfirmed=true,YellowThreshold=2,YellowBan=1,RedBan=1,SecondYellowBan=1},"match-coach");
            var nextLeagueId=await db.Leagues.Where(l=>l.Name=="Next season").Select(l=>l.Id).SingleAsync();
            Check(!(await new FootballService(db).Alerts(teamId,"Official",BillingClock.Today.AddDays(12),null,0,nextLeagueId)).Any(),"New league season does not inherit earlier suspension");
            var previous=await db.Leagues.SingleAsync(l=>l.Id==leagueId);
            await Reject(()=>new LeagueService(db).Save(new(){Id=leagueId,Revision=previous.Revision,Name=previous.Name,StartsOn=previous.StartsOn,EndsOn=previous.EndsOn,TeamIds=[]},"match-coach"),"League cannot remove a team with registered matches");
            await new LeagueService(db).Save(new(){Name="Default rules",StartsOn=BillingClock.Today,EndsOn=BillingClock.Today.AddDays(30)},"match-coach");
            Check(await db.Leagues.AnyAsync(l=>l.Name=="Default rules"&&l.RedBan==1&&l.YellowThreshold==3),"Every competition receives the same default card rules");
            var catalog=new CompetitionCatalogModel{Season="2098/99",StartsOn=new(2098,7,1),EndsOn=new(2099,6,30),SelectedKeys=["fifa-cwc"]};
            Check(await new LeagueService(db).AddFromCatalog(catalog,"match-coach")==1,"Admin adds only checked catalog competitions");
            await Reject(()=>new LeagueService(db).AddFromCatalog(catalog,"match-coach"),"Catalog prevents the same competition twice in one season");
        }
        await using(var db=Db(2)){
            Check(!await db.Leagues.AnyAsync(l=>l.Id==leagueId),"League data hidden from other clubs");
            await Reject(()=>new LeagueService(db).Save(new(){Name="Foreign",TeamIds=[teamId]},"match-coach"),"League cannot enroll another club's team");
        }
        await using(var db=Db()){stale=await new MatchService(db,false).Load("match-coach",gameId,0);}
        await using(var db=Db()){var service=new MatchService(db,false);var current=await service.Load("match-coach",gameId,0);current.Opponent="Updated visitors";await service.Save("match-coach",current);}
        await using(var db=Db()){await Reject(()=>new MatchService(db,false).Save("match-coach",stale),"Stale match edit cannot overwrite another coach's work");}
        await using(var db=Db()){
            var assignment=new CareAssignment{StudentId=first,StaffId="match-other",Kind=Roles.Doctor,AssignedBy="match-coach"};
            db.CareEntries.Add(new(){Assignment=assignment,RequestId=Guid.NewGuid(),Date=BillingClock.Today.AddDays(-2),Subject="Rest",TrainingStatus="Unavailable",CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();
        }
        await using(var db=Db()){await Reject(()=>new MatchService(db,false).Save("match-coach",Game()),"Medical unavailability prevents playing in matches");}
        await using(var db=Db()){
            var team=await db.TrainingTeams.SingleAsync(t=>t.Id==teamId);team.CoachId="match-other";await db.SaveChangesAsync();
            await Reject(()=>new MatchService(db,false).Load("match-coach",gameId,0),"Former coach loses access to match records");
        }
        await using(var db=Db()){
            var role=await db.Roles.SingleAsync(r=>r.Name==Roles.Coach);
            foreach(var actor in new[]{"match-coach","match-other"})db.UserRoles.Add(new(){UserId=actor,RoleId=role.Id});
            var date=new DateOnly(2001,1,10);var game=new MatchGame{TrainingTeamId=teamId,UpdatedById="match-coach",Date=date,Opponent="Activity fixture"};
            var snapshot=new MatchInput{Status="Draft",Players=[new(){StudentId=first,Role="Starter"}]};
            void History(string actor,int days){db.MatchChanges.Add(new(){MatchGame=game,ActorId=actor,CreatedAt=BillingClock.UtcDate(date.AddDays(days)),Snapshot=System.Text.Json.JsonSerializer.Serialize(snapshot)});}
            History("match-coach",0);snapshot.Status="Live";History("match-coach",1);
            snapshot.Status="Completed";snapshot.ScoreFor=1;snapshot.ScoreAgainst=0;History("match-other",2);History("match-coach",3);
            await db.SaveChangesAsync();
            var report=await new CoachActivityService(db).Load(date,date.AddDays(3));
            var a=report.Rows.Single(r=>r.Name=="match-coach@example.test");var b=report.Rows.Single(r=>r.Name=="match-other@example.test");
            Check(a.Lineups==1&&a.Matches==0&&a.Points==1&&b.Lineups==0&&b.Matches==1&&b.Points==1,"Activity attributes lineup and completion to their first actual actors");
            Check(a.Days==1&&a.Last==BillingClock.UtcDate(date.AddDays(1)),"Match activity contributes to active days and last registration");
            Check((await new CoachActivityService(db).Load(date.AddDays(3),date.AddDays(3))).Rows.All(r=>r.Points==0),"Later match corrections do not earn points in a new reporting period");
            Check((await new CoachActivityService(db).Load(date,date)).Rows.All(r=>r.Points==0),"Unconfirmed draft lineup earns no activity points");
        }
        await using(var db=Db(2)){Check((await new CoachActivityService(db).Load(new(2001,1,10),new(2001,1,13))).Rows.All(r=>r.Lineups==0&&r.Matches==0),"Match activity remains isolated by club");}    }
}
