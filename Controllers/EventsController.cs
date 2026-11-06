using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Read)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class EventsController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(string? status,string? search,string view="table")
    {
        var q=db.ClubEvents.AsNoTracking().Include(x=>x.Category).Include(x=>x.Invitations).AsQueryable();
        if(!string.IsNullOrEmpty(status))q=q.Where(x=>x.Status==status);
        if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>EF.Functions.ILike(x.Name,"%"+search+"%"));
        ViewData["Mode"]=view;ViewData["Status"]=status;ViewData["Search"]=search;
        return View(await q.OrderByDescending(x=>x.StartsAt).ToListAsync());
    }
    [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> Categories()=>View(await db.EventCategories.OrderBy(x=>x.Kind).ThenBy(x=>x.Name).ToListAsync());
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Category(int id,string name,string kind,bool active)
    {
        try{await new ClubEventService(db).Category(id,name??"",kind,active);TempData["Success"]="Kategoria u ruajt.";}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Categories));
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> DefaultCategories(){await new ClubEventService(db).SeedCategories();return RedirectToAction(nameof(Categories));}
    private async Task Choices(EventEdit m)
    {
        m.Categories=await db.EventCategories.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
        m.Teams=await db.TrainingTeams.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
        m.Students=await db.Students.Where(x=>x.IsActive).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToListAsync();
    }
    [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> Edit(int id=0)
    {
        var m=new EventEdit();
        if(id!=0){
            var e=await db.ClubEvents.SingleOrDefaultAsync(x=>x.Id==id);if(e==null)return NotFound();
            if(e.Status is "Cancelled" or "Completed")return RedirectToAction(nameof(Details),new{id});
            m=new(){Id=e.Id,Revision=e.Revision,Name=e.Name,Description=e.Description,CategoryId=e.CategoryId,StartsAt=ClubEventService.Local(e.StartsAt),EndsAt=ClubEventService.Local(e.EndsAt),Deadline=ClubEventService.Local(e.Deadline),Location=e.Location,Capacity=e.Capacity,IsPaid=e.Price>0,Price=e.Price,RequiresConsent=e.RequiresConsent,Terms=e.Terms,Transport=e.Transport,Instructions=e.Instructions,Organizer=e.Organizer,Contact=e.Contact,CancellationPolicy=e.CancellationPolicy};
        }
        await Choices(m);return View(m);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin),RequestSizeLimit(262144)]
    public async Task<IActionResult> Edit(EventEdit m,IFormFile? photo)
    {
        if(ModelState.IsValid)try{var id=await new ClubEventService(db).Save(m,photo,Actor);TempData["Success"]="Eventi u ruajt.";return RedirectToAction(nameof(Details),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException or ArgumentException){ModelState.AddModelError("",e.Message);}
        await Choices(m);return View(m);
    }
    public async Task<IActionResult> Details(int id)
    {
        var e=await db.ClubEvents.AsNoTracking().Include(x=>x.Category).Include(x=>x.Invitations).ThenInclude(x=>x.Student).Include(x=>x.Invitations).ThenInclude(x=>x.Consents).SingleOrDefaultAsync(x=>x.Id==id);
        if(e==null)return NotFound();
        var m=new EventDetails{Event=e,Expenses=await db.EventExpenseLinks.AsNoTracking().Include(x=>x.Expense).Where(x=>x.ClubEventId==id).ToListAsync(),
            Sponsors=await db.EventSponsorLinks.AsNoTracking().Include(x=>x.Receipt).ThenInclude(x=>x.Contract).Where(x=>x.ClubEventId==id).ToListAsync(),
            Settlements=await db.EventSettlements.AsNoTracking().Where(x=>x.Invitation.ClubEventId==id).ToListAsync(),
            Payments=await db.Payments.AsNoTracking().Where(x=>x.EventInvitationId!=null&&x.EventInvitation!.ClubEventId==id).ToListAsync(),
            Audits=await db.EventAudits.AsNoTracking().Where(x=>x.ClubEventId==id).OrderByDescending(x=>x.Id).ToListAsync()};
        if(User.IsInRole(Roles.Admin)){
            m.AvailableExpenses=await db.Expenses.AsNoTracking().Where(x=>x.CancelledAt==null&&!db.EventExpenseLinks.Any(l=>l.ExpenseId==x.Id)&&!db.EventSettlements.Any(s=>s.ExpenseId==x.Id)).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
            m.AvailableSponsors=await db.SponsorshipReceipts.AsNoTracking().Include(x=>x.Contract).Where(x=>x.CancelledAt==null&&!db.EventSponsorLinks.Any(l=>l.SponsorshipReceiptId==x.Id)).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
            m.ExpenseCategories=await db.ExpenseCategories.AsNoTracking().Where(x=>x.IsActive&&x.ParentId==null&&x.ContextType==null).OrderBy(x=>x.Name).ToListAsync();
        }
        return View(m);
    }
    public async Task<IActionResult> Photo(int id)
    {
        var e=await db.ClubEvents.AsNoTracking().Where(x=>x.Id==id).Select(x=>new{x.Photo,x.PhotoType}).SingleOrDefaultAsync();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return e?.Photo==null?NotFound():File(e.Photo,e.PhotoType??"image/webp");
    }
    public async Task<IActionResult> Print(int id)
    {
        var e=await db.ClubEvents.AsNoTracking().Include(x=>x.Invitations).ThenInclude(x=>x.Student).Include(x=>x.Invitations).ThenInclude(x=>x.Consents).SingleOrDefaultAsync(x=>x.Id==id);
        return e==null?NotFound():View(e);
    }
    private async Task<IActionResult> Act(int id,Func<Task<int>> action)
    {
        try{id=await action();TempData["Success"]="Veprimi u ruajt.";}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Details),new{id});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> State(int id,Guid revision,string command)=>Act(id,()=>new ClubEventService(db).State(id,revision,command,Actor));
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> Withdrawal(int id,int invitationId,bool approve,string reason)=>Act(id,()=>new ClubEventService(db).ResolveWithdrawal(invitationId,approve,reason??"",Actor));
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> CancelParticipation(int id,int invitationId,string reason)=>Act(id,()=>new ClubEventService(db).CancelParticipation(invitationId,reason??"",Actor));
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)] public Task<IActionResult> Attendance(int id,int invitationId,bool attended)=>Act(id,()=>new ClubEventService(db).Attendance(invitationId,attended,Actor));
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)] public async Task<IActionResult> Pay(int id,int invitationId,decimal amount,string method,Guid request)
    {
        try{
            if(method=="Bank"){await new BankVerificationService(db).QueueEvent(new(){InvitationId=invitationId,Amount=amount,RequestId=request},Actor);TempData["Success"]="Pagesa bankare u dërgua për verifikim.";return RedirectToAction(nameof(Details),new{id});}
            var receipt=await new ClubEventService(db).Pay(invitationId,amount,method,request,Actor);return RedirectToAction("Receipt","Payments",new{id=receipt});
        }
        catch(Exception e)when(e is ValidationException or InvalidOperationException){TempData["Error"]=e.Message;return RedirectToAction(nameof(Details),new{id});}
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> Settle(int id,int invitationId,decimal amount,string kind,string method,Guid request,string reason)=>Act(id,async()=>{
        if(kind=="Refund"&&method=="Bank"){
            await new BankVerificationService(db).QueueEvent(new(){InvitationId=invitationId,Amount=amount,RequestId=request,Refund=true,Reason=reason??""},Actor);return id;
        }
        return await new ClubEventService(db).Settle(invitationId,amount,kind,method,request,reason??"",Actor);
    });
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> Link(int id,int sourceId,bool sponsor)=>Act(id,()=>new ClubEventService(db).Link(id,sourceId,sponsor,Actor));
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)] public Task<IActionResult> Expense(int id,ExpenseCreateModel input)=>Act(id,()=>new ClubEventService(db).Expense(id,input,Actor));
}
