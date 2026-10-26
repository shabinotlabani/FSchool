using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

static class ChatChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        ApplicationDbContext Db(int club=1)=>new(options,new ClubContext{ClubId=club});
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
        static async Task Reject(Func<Task> action,string text){try{await action();}catch(Exception e)when(e is UnauthorizedAccessException or ValidationException or InvalidOperationException){Console.WriteLine("PASS: "+text);return;}throw new Exception(text);}
        int studentId,teamId,otherId;Guid revision;
        var token=new string('c',64);var otherToken=new string('d',64);
        await using(var db=Db()){
            var role=await db.Roles.SingleAsync(x=>x.Name==Roles.Coach);
            foreach(var id in new[]{"chat-coach","chat-assistant","chat-outsider"}){
                db.Users.Add(new ApplicationUser{Id=id,UserName=id+"@example.test",FirstName=id});
                db.UserRoles.Add(new IdentityUserRole<string>{UserId=id,RoleId=role.Id});
            }
            var team=new TrainingTeam{Name="Chat U12",CoachId="chat-coach",AssistantCoachId="chat-assistant",MinAge=0,MaxAge=99,Capacity=30};
            db.TrainingTeams.Add(team);
            var student=new Student{FirstName="Chat",LastName="Player",DateOfBirth=new(2015,1,1),TrainingTeam=team,ParentLinkToken=token};
            db.Students.Add(student);await db.SaveChangesAsync();studentId=student.Id;teamId=team.Id;revision=student.Revision;
            db.StudentTransactions.Add(new(){StudentId=studentId,TransactionType="Test",Description="Existing obligation",Debit=50});
            var family=new PlayerFamily{Name="Chat Family",ParentLinkToken=new string('9',64)};db.PlayerFamilies.Add(family);
            db.StudentFeeAssignments.Add(new(){StudentId=studentId,Family=family,FamilyOrder=1,FeePlanId=2,EffectiveMonth=TariffService.CurrentMonth});
            await db.SaveChangesAsync();
        }
        await using(var db=Db(2)){
            var student=new Student{FirstName="Other",LastName="Club",DateOfBirth=new(2015,1,1),ParentLinkToken=otherToken};
            db.Students.Add(student);await db.SaveChangesAsync();otherId=student.Id;
        }
        var message=new ChatInput{StudentId=studentId,CoachId="chat-coach",Body="A mund te flasim per stervitjen?"};
        await using(var db=Db()){
            var roster=new CoachPlayersService(db);
            Check((await roster.Load("chat-coach",null)).Players.Single().Percent==null,"Coach roster has no artificial zero for unrecorded attendance");
            Check((await roster.Load("chat-assistant",null)).Players.Single().Id==studentId,"Assistant sees assigned players");
            Check((await roster.Load("chat-outsider",null)).Players.Count==0,"Unassigned coach cannot list players");
            Check((await roster.Load("chat-coach",null)).Players.Single().Rating==null,"Roster does not invent stars for unevaluated players");
            db.PlayerEvaluations.AddRange(
                new(){StudentId=studentId,CoachId="chat-coach",Date=BillingClock.Today.AddDays(-2),Technique=1,Tactics=1,Fitness=1,Discipline=1,Teamwork=1},
                new(){StudentId=studentId,CoachId="chat-coach",Date=BillingClock.Today.AddDays(-1),Technique=5,Tactics=4,Fitness=3,Discipline=4,Teamwork=5},
                new(){StudentId=studentId,CoachId="chat-coach",Sport="Basketball",Date=BillingClock.Today,Technique=1,Tactics=1,Fitness=1,Discipline=1,Teamwork=1});
            foreach(var entry in new[]{(Days:1,Role:"head",Present:true),(Days:2,Role:"head",Present:false),(Days:40,Role:"head",Present:true)})
                db.TeamAttendances.Add(new(){TrainingTeamId=teamId,Date=BillingClock.Today.AddDays(-entry.Days),CoachRole=entry.Role,SavedById="chat-coach",Players=[new(){StudentId=studentId,StudentName="Chat Player",Present=entry.Present}]});
            await db.SaveChangesAsync();
            var row=(await roster.Load("chat-coach","Chat")).Players.Single();
            Check(row.Total==2&&row.Present==1&&row.Percent==50,"Roster percentage counts registered days and excludes old records");
            Check(row.Rating==4.2m&&row.RatingDate==BillingClock.Today.AddDays(-1),"Roster stars average the latest assessment for the current sport");
        }
        await using(var db=Db(2))Check((await new CoachPlayersService(db).Load("chat-coach",null)).Players.Count==0,"Coach roster is isolated by club");
        await using(var db=Db()){
            var service=new CoachChatService(db);
            Check((await service.Recipients(studentId)).Count==2,"Parent can choose assigned coach or assistant");
            Check((await service.Load(studentId,"chat-coach",new string('9',64))).Student.Id==studentId,"Family private link opens its child's conversation");
            await service.Send(message,"chat-coach",token);await service.Send(message,"chat-coach",token);
            Check(await db.CoachMessages.CountAsync(x=>x.StudentId==studentId)==1,"Repeated parent send records one message");
            Check((await service.Inbox("chat-coach")).Single().Unread==1,"Parent message appears unread in coach inbox");
            Check((await service.Inbox("chat-assistant")).Count==0,"Private conversation is not exposed to assistant");
            await Reject(()=>service.Load(studentId,"chat-outsider",null),"Unassigned coach cannot read chat");
            await Reject(()=>service.Send(new(){StudentId=studentId,Body="forged"},"chat-coach",otherToken),"Foreign parent token cannot send a message");
            await Reject(()=>service.Load(otherId,"chat-coach",token),"Foreign club player cannot be opened by ID");
            await Reject(()=>service.Send(new(){StudentId=studentId,Body=" "},"chat-coach",token),"Empty chat message rejected");
            await Reject(()=>service.Send(new(){StudentId=studentId,Body=new string('x',2001)},"chat-coach",token),"Oversized chat message rejected");
            var loaded=await service.Load(studentId,"chat-coach",null);
            await service.Read(studentId,"chat-coach",null,loaded.Messages.Last().Id);
            Check((await service.Inbox("chat-coach")).Single().Unread==0,"Reading conversation clears incoming unread count");
            await service.Send(new(){StudentId=studentId,Body="Po, shihemi neser."},"chat-coach",null);
            Check((await service.Load(studentId,"chat-coach",token)).Messages.Count==2,"Parent sees coach reply");
            await service.Read(studentId,"chat-coach",token,long.MaxValue);
            Check(await db.CoachMessages.AllAsync(x=>x.ReadAt!=null),"Read receipts apply to the recipient's messages");
            await Reject(()=>new StudentWorkflowService(db).DeactivateByCoach(new(){StudentId=studentId,Revision=revision,Reason=""},"chat-coach"),"Deactivation requires a reason");
            await Reject(()=>new StudentWorkflowService(db).DeactivateByCoach(new(){StudentId=studentId,Revision=revision,Reason="Test"},"chat-outsider"),"Unassigned coach cannot deactivate player");
        }
        await using(var db=Db(2))Check(!await db.CoachMessages.AnyAsync(),"Other club cannot see chat messages");
        await using(var db=Db()){
            var stale=await new AttendanceService(db).LoadAsync(teamId,"head",BillingClock.Today,"chat-coach");
            await new StudentWorkflowService(db).DeactivateByCoach(new(){StudentId=studentId,Revision=revision,Reason="Pushim i kerkuar nga prindi"},"chat-coach");
            Check(!await db.Students.Where(x=>x.Id==studentId).Select(x=>x.IsActive).SingleAsync(),"Coach deactivates assigned player");
            Check((await new CoachPlayersService(db).Load("chat-coach",null)).Players.Count==0,"Coach roster excludes inactive players");
            Check(await db.StudentChanges.AnyAsync(x=>x.StudentId==studentId&&x.ActorId=="chat-coach"&&x.Reason=="Pushim i kerkuar nga prindi"),"Deactivation preserves actor and reason in audit");
            Check(!(await new AttendanceService(db).LoadAsync(teamId,"head",BillingClock.Today,"chat-coach")).Players.Any(x=>x.StudentId==studentId),"Inactive player is removed from live attendance roster");
            foreach(var p in stale.Players)p.Present=true;
            await Reject(()=>new AttendanceService(db).SaveAsync(stale,"chat-coach"),"Stale attendance cannot mark deactivated player present");
            Check(await db.CoachMessages.CountAsync(x=>x.StudentId==studentId)==2,"Deactivation preserves chat history");
            Check(await db.StudentTransactions.AnyAsync(x=>x.StudentId==studentId&&x.Debit==50&&!x.IsCancelled),"Deactivation preserves existing financial obligations");
            await Reject(()=>new StudentWorkflowService(db).DeactivateByCoach(new(){StudentId=studentId,Revision=revision,Reason="Again"},"chat-coach"),"Stale deactivation does not duplicate audit");
        }
        await using(var db=Db()){
            var team=await db.TrainingTeams.SingleAsync(x=>x.Id==teamId);team.CoachId="chat-outsider";await db.SaveChangesAsync();
            await Reject(()=>new CoachChatService(db).Load(studentId,"chat-coach",null),"Former coach loses access after reassignment");
            Check((await new CoachChatService(db).Load(studentId,"chat-outsider",token)).Messages.Count==0,"Replacement coach does not inherit private chat");
            var student=await db.Students.SingleAsync(x=>x.Id==studentId);student.ParentLinkToken=new string('e',64);await db.SaveChangesAsync();
            await Reject(()=>new CoachChatService(db).Load(studentId,"chat-outsider",token),"Revoking parent link also revokes chat access");
        }
    }
}
