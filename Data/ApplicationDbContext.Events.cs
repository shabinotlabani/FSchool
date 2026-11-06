using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;
public partial class ApplicationDbContext
{
    public DbSet<ClubEvent> ClubEvents => Set<ClubEvent>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<EventInvitation> EventInvitations => Set<EventInvitation>();
    public DbSet<EventConsent> EventConsents => Set<EventConsent>();
    public DbSet<EventAudit> EventAudits => Set<EventAudit>();
    public DbSet<EventExpenseLink> EventExpenseLinks => Set<EventExpenseLink>();
    public DbSet<EventSponsorLink> EventSponsorLinks => Set<EventSponsorLink>();
    public DbSet<EventSettlement> EventSettlements => Set<EventSettlement>();
    private static void ConfigureEvents(ModelBuilder b)
    {
        b.Entity<ClubEvent>().Property(x=>x.Revision).IsConcurrencyToken();
        b.Entity<EventCategory>().HasIndex(x=>x.Name).IsUnique();
        b.Entity<EventInvitation>().HasIndex(x=>new{x.ClubEventId,x.StudentId}).IsUnique();
        b.Entity<EventExpenseLink>().HasIndex(x=>x.ExpenseId).IsUnique();
        b.Entity<EventSponsorLink>().HasIndex(x=>x.SponsorshipReceiptId).IsUnique();
        b.Entity<EventSettlement>().HasIndex(x=>x.RequestId).IsUnique();
        b.Entity<ClubEvent>().HasOne(x=>x.Category).WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventInvitation>().HasOne(x=>x.Event).WithMany(x=>x.Invitations).HasForeignKey(x=>x.ClubEventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventInvitation>().HasOne(x=>x.Student).WithMany().HasForeignKey(x=>x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventConsent>().HasOne(x=>x.Invitation).WithMany(x=>x.Consents).HasForeignKey(x=>x.EventInvitationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventAudit>().HasOne(x=>x.Event).WithMany().HasForeignKey(x=>x.ClubEventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().HasOne(x=>x.EventInvitation).WithMany().HasForeignKey(x=>x.EventInvitationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventExpenseLink>().HasOne(x=>x.Event).WithMany().HasForeignKey(x=>x.ClubEventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventExpenseLink>().HasOne(x=>x.Expense).WithMany().HasForeignKey(x=>x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventSponsorLink>().HasOne(x=>x.Event).WithMany().HasForeignKey(x=>x.ClubEventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventSponsorLink>().HasOne(x=>x.Receipt).WithMany().HasForeignKey(x=>x.SponsorshipReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventSettlement>().HasOne(x=>x.Invitation).WithMany().HasForeignKey(x=>x.EventInvitationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EventSettlement>().HasOne(x=>x.Expense).WithMany().HasForeignKey(x=>x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
    }
}
