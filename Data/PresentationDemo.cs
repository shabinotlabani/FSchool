using System.Text.Json;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;

public static class PresentationDemo
{
    public const string ImportId="presentation-2026-10-v1";
    public const string Note="DEMO · Të dhëna fiktive për prezantim";
    public static async Task<bool> SeedAsync(IServiceProvider services)
    {
        using var scope=services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Club.ClubId=1;
        var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2007)");
        if(await db.DemoImports.AnyAsync(x=>x.Id==ImportId))return false;
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var admin=(await users.GetUsersInRoleAsync(Roles.Admin)).FirstOrDefault()??throw new InvalidOperationException("Administrator required for demo import.");
        var actor=admin.Id;var today=BillingClock.Today;var month=TariffService.CurrentMonth;var now=DateTime.UtcNow;
        var coaches=new List<ApplicationUser>();
        foreach(var name in new[]{"Ardit","Blerim","Driton","Valon"}){
            var coach=new ApplicationUser{UserName=$"demo.{name.ToLowerInvariant()}@example.invalid",Email=$"demo.{name.ToLowerInvariant()}@example.invalid",FirstName=name,LastName="DEMO",EmailConfirmed=true,SecurityStamp=Guid.NewGuid().ToString()};
            // Demo staff are assignment records, not publicly usable login accounts.
            var result=await users.CreateAsync(coach);if(!result.Succeeded)throw new InvalidOperationException("Demo coach collision: "+name);
            result=await users.AddToRoleAsync(coach,Roles.Coach);if(!result.Succeeded)throw new InvalidOperationException("Demo coach role failed.");
            coaches.Add(coach);
        }
        var fields=Enumerable.Range(1,3).Select(i=>new FootballField{Name=$"DEMO · Fusha {i}",Location="Qendra sportive DEMO",Notes=Note}).ToList();
        db.FootballFields.AddRange(fields);await db.SaveChangesAsync();
        var teams=new List<TrainingTeam>();
        for(var i=0;i<3;i++){
            var team=new TrainingTeam{Name=$"DEMO · U{10+i*2}",MinAge=7+i*2,MaxAge=10+i*2,Capacity=20,CoachId=coaches[i].Id,AssistantCoachId=coaches[(i+1)%4].Id,FootballFieldId=fields[i].Id,Location=fields[i].Location,Notes=Note,
                Sessions=new[]{1,3,5}.Select(day=>new TrainingSession{Day=day,StartsAt=new(16+i,0),EndsAt=new(17+i,0)}).ToList()};
            db.TrainingTeams.Add(team);teams.Add(team);
        }
        await db.SaveChangesAsync();
        foreach(var team in teams){
            db.CoachTeams.AddRange(new CoachTeam{TrainingTeamId=team.Id,UserId=team.CoachId!},new CoachTeam{TrainingTeamId=team.Id,UserId=team.AssistantCoachId!});
            db.TeamChanges.Add(new(){TrainingTeamId=team.Id,ActorId=actor,Reason=Note,Snapshot=team.Name});
        }
        var individual=new FeePlan{Name="DEMO · Individuale 50"};var familyPlan=new FeePlan{Name="DEMO · Familjare",IsFamily=true};
        db.FeePlans.AddRange(individual,familyPlan);await db.SaveChangesAsync();
        db.FeePlanPrices.AddRange(new(){FeePlanId=individual.Id,EffectiveMonth=month,Amount=50,Reason=Note,CreatedByUserId=actor},new(){FeePlanId=familyPlan.Id,EffectiveMonth=month,Amount=35,FamilyFirstAmount=35,FamilyAdditionalAmount=25,FamilyFirstCount=2,SingleUsesStandard=false,Reason=Note,CreatedByUserId=actor});
        var families=new[]{new PlayerFamily{Name="DEMO · Familja Berisha"},new PlayerFamily{Name="DEMO · Familja Krasniqi",PreferredPaymentMethod="Bank"}};
        db.PlayerFamilies.AddRange(families);await db.SaveChangesAsync();
        foreach(var family in families){db.FamilyTariffAssignments.Add(new(){FamilyId=family.Id,FeePlanId=familyPlan.Id,EffectiveMonth=month});db.FamilyChanges.Add(new(){FamilyId=family.Id,EffectiveMonth=month,Reason=Note,Snapshot=family.Name,ActorId=actor});}
        var names=new[]{"Arben","Besim","Diar","Endrit","Faton","Gent","Ilir","Luan","Mal","Nori","Orik","Rron","Arta","Diona","Elira","Era","Leon","Yll"};
        var players=new List<Student>();
        for(var i=0;i<names.Length;i++){
            var student=new Student{FirstName=names[i],LastName="DEMO",DateOfBirth=today.AddYears(-(8+i/6*2)).AddDays(-20),RegistrationDate=BillingClock.UtcDate(month),MonthlyFee=i<4?35:50,TrainingTeamId=teams[i/6].Id,ParentName="Prindi DEMO "+(i+1),Notes=Note+(i==5?" · Merr pjesë edhe në trajnim personal":""),PreferredPaymentMethod=i is 2 or 3 or 5 or 9?"Bank":"Cash"};
            db.Students.Add(student);players.Add(student);
        }
        await db.SaveChangesAsync();
        for(var i=0;i<players.Count;i++){
            db.StudentFeeAssignments.Add(new(){StudentId=players[i].Id,FeePlanId=i<4?familyPlan.Id:individual.Id,FamilyId=i<4?families[i/2].Id:null,FamilyOrder=i<4?i%2+1:null,EffectiveMonth=month,Reason=Note,CreatedByUserId=actor});
            db.StudentTeamChanges.Add(new(){StudentId=players[i].Id,ToTeamId=players[i].TrainingTeamId,ToTeamName=teams[i/6].Name,Reason=Note,ActorId=actor});
        }
        await db.SaveChangesAsync();
        var billing=new BillingService(db);
        // The seasonal player is registered directly into the season, before any monthly invoice.
        await billing.RegisterSeasonalAsync(new(){StudentId=players[6].Id,StartMonth=month.ToString("yyyy-MM"),Months=6,ExpectedRate=50,PaymentMethod="Cash",Notes=Note},actor);
        foreach(var player in players)await billing.GenerateStudentMonthAsync(player.Id,today.Year,today.Month,actor);
        foreach(var index in new[]{4,7,8,11,12})await billing.RegisterPaymentAsync(new(){StudentId=players[index].Id,Amount=index==7?100:index==8?25:50,Notes=Note},actor);
        var familyAccount=(await billing.GetFamilyAccountAsync(families[0].Id))!;
        await billing.RegisterFamilyPaymentAsync(new(){FamilyId=families[0].Id,Amount=familyAccount.Due,ExpectedState=familyAccount.State,Notes=Note},actor);
        var bank=new BankVerificationService(db);
        var pending=await db.PendingBankPayments.SingleAsync(x=>x.StudentId==players[5].Id&&x.Kind=="Monthly"&&x.ReviewedAt==null);
        // Seed a confirmed bank receipt using the normal posting path. Other bank requests remain pending.
        pending.PaymentId=await billing.RegisterPaymentAsync(new(){StudentId=players[5].Id,Amount=50,PaymentMethod="Bank",Notes=Note},actor);
        pending.ReviewedAt=now;pending.ReviewedById=actor;pending.ConfirmedAmount=50;pending.RejectionReason=null;await db.SaveChangesAsync();
        var cancelled=await billing.RegisterPaymentAsync(new(){StudentId=players[10].Id,Amount=20,Notes=Note},actor);
        await billing.CancelPaymentAsync(cancelled,"DEMO · Pagesë e regjistruar gabimisht",actor);
        var invoice=(await billing.GetInvoicesAsync(players[13].Id)).Single();
        await billing.ExemptAsync(new(){PeriodId=invoice.Period.Id,Amount=25,Reason="DEMO · Mungesë e arsyetuar",ExemptionType="Vacation",Notes=Note},actor);
        var invoice2=(await billing.GetInvoicesAsync(players[14].Id)).Single();
        await billing.ExemptAsync(new(){PeriodId=invoice2.Period.Id,Amount=50,Reason="DEMO · Pushim për një muaj",ExemptionType="Vacation",Notes=Note},actor);
        // Saved attendance stays historical; today's sheet remains ready for a live presentation.
        foreach(var team in teams){
            var roster=players.Where(p=>p.TrainingTeamId==team.Id).ToList();
            for(var offset=1;offset<=14;offset++){
                var date=today.AddDays(-offset);var weekday=((int)date.DayOfWeek+6)%7+1;
                if(date<month||!team.Sessions.Any(s=>s.Day==weekday))continue;
                var sheet=new TeamAttendance{TrainingTeamId=team.Id,Date=date,SavedById=team.CoachId!,CoachRole="head",UpdatedAt=BillingClock.UtcDate(date).AddHours(17),Players=roster.Select((p,i)=>new AttendancePlayer{StudentId=p.Id,StudentName=p.FullName,Present=(i+offset)%5!=0}).ToList()};
                db.TeamAttendances.Add(sheet);db.AttendanceChanges.Add(new(){TeamAttendance=sheet,ActorId=team.CoachId!,CreatedAt=sheet.UpdatedAt,Snapshot=JsonSerializer.Serialize(new{CoachRole="head",Players=sheet.Players.Select(p=>new{p.StudentId,p.StudentName,p.Present})})});
            }
        }
        await db.SaveChangesAsync();
        await SeedStock(db,players,actor);
        var personal=new PersonalTrainingService(db);
        var tariff=new PersonalTariff{Name="DEMO · Teknikë individuale",Mode="Session",Amount=20,Notes=Note};db.PersonalTariffs.Add(tariff);await db.SaveChangesAsync();
        for(var i=0;i<3;i++){
            var trainingId=await personal.CreateAsync(new(){StudentId=players[15+i].Id,PersonalTariffId=tariff.Id,ExpectedRate=20,CoachId=coaches[i].Id,StartsOn=today.AddDays(1),Notes=Note,Sessions=[new(){Day=((int)today.AddDays(1).DayOfWeek+6)%7+1,StartsAt=new(10+i,0),EndsAt=new(11+i,0)}]},actor);
            var charge=await db.PersonalCharges.SingleAsync(x=>x.PersonalTrainingId==trainingId);
            if(i==0)await personal.PayAsync(new(){ChargeId=charge.Id,Amount=20,Notes=Note},actor);
            if(i==1)await bank.QueuePersonal(new(){ChargeId=charge.Id,Amount=20,Notes=Note},actor);
        }
        for(var i=0;i<3;i++){
            var booking=new FieldBooking{RequestId=Guid.NewGuid(),FootballFieldId=fields[i].Id,CustomerName=$"DEMO · Termini privat {i+1}",Date=today.AddDays(1),StartsAt=new(20,0),EndsAt=new(21,0),Price=60,Notes=Note};
            booking.Changes.Add(new(){ActorId=actor,Description=Note});
            if(i==0)booking.Payments.Add(new(){RequestId=Guid.NewGuid(),Amount=60,ActorId=actor,Description=Note});
            if(i==2){booking.IsCancelled=true;booking.CancellationReason="DEMO · Klienti ndryshoi planin";booking.Changes.Add(new(){ActorId=actor,Description="Anuluar: "+booking.CancellationReason});}
            db.FieldBookings.Add(booking);
        }
        var category=new ExpenseCategory{Name="DEMO · Shpenzime sportive"};db.ExpenseCategories.Add(category);await db.SaveChangesAsync();
        db.Expenses.AddRange(new(){RequestId=Guid.NewGuid(),CategoryId=category.Id,CategoryName=category.Name,Date=today,Amount=30,Description="DEMO · Mirëmbajtje",Method="Cash",CreatedById=actor,Notes=Note},new(){RequestId=Guid.NewGuid(),CategoryId=category.Id,CategoryName=category.Name,Date=today,Amount=15,Description="DEMO · Transport",Method="Bank",CreatedById=actor,Notes=Note},new(){RequestId=Guid.NewGuid(),CategoryId=category.Id,CategoryName=category.Name,Date=today,Amount=10,Description="DEMO · Shpenzim i anuluar",Method="Cash",CreatedById=actor,Notes=Note,CancelledAt=now,CancelledById=actor,CancellationReason="DEMO · Dokument i dyfishtë"});
        // Never replace a real opening balance; only demonstrate a small owner withdrawal.
        db.TreasuryEntries.Add(new(){RequestId=Guid.NewGuid(),Kind=TreasuryEntry.Withdrawal,Method="Cash",Date=today,Amount=20,Owner="Pronar DEMO",Notes=Note,CreatedById=actor});
        db.DemoImports.Add(new(){Id=ImportId,CompletedAt=now,Students=players.Count,Teams=teams.Count});
        await db.SaveChangesAsync();await tx.CommitAsync();return true;
    }
    private static async Task SeedStock(ApplicationDbContext db,List<Student> players,string actor)
    {
        var items=new[]{("Fanellë","Veshje",25m), ("Tuta","Veshje",40m),("Top futbolli","Pajisje sportive",20m),("Çantë","Aksesorë",15m)};
        var products=new List<Product>();var entry=new StockEntry{RequestId=Guid.NewGuid(),DocumentNumber="DEMO-HYRJE-01",SupplierName="Furnizues DEMO",Notes=Note,CreatedByUserId=actor};
        db.StockEntries.Add(entry);await db.SaveChangesAsync();
        for(var i=0;i<items.Length;i++){
            var product=new Product{Code="DEMO-"+(i+1),Name="DEMO · "+items[i].Item1,CategoryId=items[i].Item2,SalePrice=items[i].Item3,PurchasePrice=10,MinimumStock=3};
            foreach(var size in i<2?new[]{"S","M","L","XL"}:new[]{"Standard"})product.Variants.Add(new(){Size=size,PurchasePrice=10,CurrentStock=size=="XL"?0:10});
            product.CurrentStock=product.Variants.Sum(v=>v.CurrentStock);db.Products.Add(product);products.Add(product);await db.SaveChangesAsync();
            foreach(var variant in product.Variants.Where(v=>v.CurrentStock>0)){
                var total=variant.CurrentStock*10;entry.TotalAmount+=total;
                db.StockEntryDetails.Add(new(){StockEntryId=entry.Id,ProductId=product.Id,ProductVariantId=variant.Id,ProductName=product.Name,ProductCode=product.Code,ProductSize=variant.Size,UnitName="Copë",Quantity=variant.CurrentStock,UnitPrice=10,Total=total});
                db.StockMovements.Add(new(){ProductId=product.Id,ProductVariantId=variant.Id,ProductSize=variant.Size,UnitName="Copë",MovementType="StockEntry",Quantity=variant.CurrentStock,PurchasePrice=10,ReferenceType="StockEntry",ReferenceId=entry.Id,UserId=actor,Notes=Note});
            }
        }
        await db.SaveChangesAsync();
        for(var i=0;i<4;i++){
            var product=products[i];await new IssueService(db).CreateAsync(new(){StudentId=players[4+i].Id,AmountPaid=i==0?product.SalePrice:i==2?product.SalePrice:0,PaymentMethod=i==2?"Bank":"Cash",WaivePayment=i==3,WaiverReason=i==3?"DEMO · Mbështetje për lojtarin":null,Notes=Note,Lines=[new(){ProductId=product.Id,ProductVariantId=product.Variants.First().Id,Quantity=1}]},actor,true);
        }
    }
}
