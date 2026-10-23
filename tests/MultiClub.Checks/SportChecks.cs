using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

static class SportChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        ApplicationDbContext Db(int club)=>new(options,new ClubContext{ClubId=club});
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
        static async Task Reject(Func<Task> action,string text){try{await action();}catch(ValidationException){Console.WriteLine("PASS: "+text);return;}throw new Exception(text);}
        int clubId,studentId;
        await using(var db=Db(1)){
            db.Club.ManagePlatform=true;var club=new ClubTenant{Name="Sport tests",Sport="Football"};db.Clubs.Add(club);await db.SaveChangesAsync();clubId=club.Id;
        }
        await using(var db=Db(clubId)){
            var user=new ApplicationUser{Id="sport-coach",UserName="sport-coach",FirstName="Sport"};db.Users.Add(user);
            db.UserRoles.Add(new IdentityUserRole<string>{UserId=user.Id,RoleId=await db.Roles.Where(x=>x.Name==Roles.Coach).Select(x=>x.Id).SingleAsync()});
            var student=new Student{FirstName="Sport",LastName="Player",DateOfBirth=new(2014,1,1),TrainingTeam=new TrainingTeam{Name="Sport team",CoachId=user.Id,IsActive=true}};
            db.Students.Add(student);await db.SaveChangesAsync();studentId=student.Id;
            await new PlayerDevelopmentService(db).SaveRecord(new(){StudentId=studentId,Kind="Skills",Values=new(){["Crossing"]="4"}},user.Id);
            Check((await new SportDevelopmentService(db).Catalog()).Roles.Contains("Portier"),"Existing football club retains goalkeeper roles");
            db.Club.ManagePlatform=true;(await db.Clubs.SingleAsync(x=>x.Id==clubId)).Sport="Basketball";await db.SaveChangesAsync();
        }
        await using(var db=Db(clubId)){
            var settings=new SportDevelopmentService(db);var catalog=await settings.Catalog();
            var rpe=(await settings.Fields("Basketball")).Single(x=>x.Section=="Load"&&x.Key=="RPE");
            Check(rpe.Type=="Scale"&&rpe.Min==0&&rpe.Max==10,"RPE retains its zero-to-ten scale in metric management");
            Check(catalog.Find("Goalkeeping")==null&&catalog.Find("KeeperTraining")==null&&!catalog.Roles.Contains("Portier"),"Basketball has no goalkeeper sections or role");
            Check(catalog.Positions.Contains("Qendër (C)")&&catalog.Find("Skills")!.Fields.Any(x=>x.Key=="Shooting")&&!catalog.Find("Skills")!.Fields.Any(x=>x.Key=="Crossing"),"Basketball uses actual positions and skills");
            var service=new PlayerDevelopmentService(db);
            await Reject(()=>service.SaveRecord(new(){Sport="Basketball",StudentId=studentId,Kind="Goalkeeping",Values=new(){["Handling"]="5"}},"sport-coach"),"Forged football section rejected for basketball");
            await Reject(()=>service.SaveRecord(new(){StudentId=studentId,Kind="Skills",Values=new(){["Passing"]="4"}},"sport-coach"),"Stale form from previous sport rejected");
            await Reject(()=>service.SaveRecord(new(){Sport="Basketball",StudentId=studentId,Kind="Profile",Values=new(){["PlayerRole"]="Portier"}},"sport-coach"),"Basketball rejects forged goalkeeper role");
            await service.SaveRecord(new(){Sport="Basketball",StudentId=studentId,Kind="Skills",Values=new(){["Shooting"]="4"}},"sport-coach");
            await settings.Save(new(){Sport="Basketball",Section="Measurements",Label="Standing reach (cm)",Type="Number",Min=50,Max=350,Step="0.1"});
            var custom=(await settings.Fields("Basketball")).Single(x=>x.Key.StartsWith("Custom"));
            await service.SaveRecord(new(){Sport="Basketball",StudentId=studentId,Kind="Measurements",Values=new(){[custom.Key]="210.5"}},"sport-coach");
            var record=await db.DevelopmentRecords.Where(x=>x.StudentId==studentId&&x.Kind=="Measurements").SingleAsync();
            Check(record.Schema.Fields.Any(x=>x.Key==custom.Key&&x.Label=="Standing reach (cm)"),"Custom measurement schema is stored with recorded value");
            await settings.Save(new(){Id=custom.Id,Revision=custom.Revision,Sport="Basketball",Section=custom.Section,Key=custom.Key,Label="Reach changed (cm)",Type="Number",Min=50,Max=400,Step="0.1",Enabled=false});
            Check(!(await settings.Catalog()).Find("Measurements")!.Fields.Any(x=>x.Key==custom.Key),"Deactivated metric disappears from new forms");
            await Reject(()=>service.SaveRecord(new(){Sport="Basketball",StudentId=studentId,Kind="Measurements",Values=new(){[custom.Key]="211"}},"sport-coach"),"Stale form cannot save deactivated metric");
            await service.SaveRecord(new(){Id=record.Id,Revision=record.Revision,StudentId=studentId,Kind="Measurements",Date=record.Date,Values=new(){[custom.Key]="212.5"}},"sport-coach");
            Check((await db.DevelopmentRecords.SingleAsync(x=>x.Id==record.Id)).Schema.Fields.Any(x=>x.Label=="Standing reach (cm)"),"Editing history preserves original measurement label and unit");
            var all=await db.DevelopmentRecords.Where(x=>x.StudentId==studentId).ToListAsync();var charts=DevelopmentCharts.Build(all);
            Check(charts.Any(x=>x.Title.StartsWith("Futboll"))&&charts.Any(x=>x.Title.StartsWith("Basketboll")),"Progress keeps different sports separate");
            Check(charts.Any(x=>x.Title.Contains("Standing reach")&&x.Points.Last().Value==212.5m),"Historical custom metric remains in progress charts");
            var parent=await new ParentProgressService(db).Load(studentId,BillingClock.Today);
            Check(parent.Any(x=>x.Title.Contains("Standing reach")),"Parent progress includes configured numeric measurements");
            await Reject(()=>settings.Save(new(){Sport="Basketball",Section="Measurements",Label="Invalid",Min=10,Max=5,Step="0"}),"Invalid metric bounds and step rejected");
        }
        await using(var db=Db(1))Check(!await db.DevelopmentMetrics.AnyAsync(x=>x.Key.StartsWith("Custom")),"Metric settings are isolated by club");
    }
}
