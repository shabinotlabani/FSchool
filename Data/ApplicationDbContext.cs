using _2Korriku.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<TreasuryEntry> TreasuryEntries => Set<TreasuryEntry>();
    public DbSet<FootballField> FootballFields => Set<FootballField>();
    public DbSet<FieldPayment> FieldPayments => Set<FieldPayment>();
    public DbSet<FieldBooking> FieldBookings => Set<FieldBooking>();
    public DbSet<FieldBookingChange> FieldBookingChanges => Set<FieldBookingChange>();
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CoachTeam> CoachTeams => Set<CoachTeam>();
    public DbSet<FirstMonthFeeChange> FirstMonthFeeChanges => Set<FirstMonthFeeChange>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseCategoryChange> ExpenseCategoryChanges => Set<ExpenseCategoryChange>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PersonalTariff> PersonalTariffs => Set<PersonalTariff>();
    public DbSet<PersonalTraining> PersonalTrainings => Set<PersonalTraining>();
    public DbSet<PersonalTrainingSlot> PersonalTrainingSlots => Set<PersonalTrainingSlot>();
    public DbSet<PersonalCharge> PersonalCharges => Set<PersonalCharge>();
    public DbSet<PersonalTrainingAudit> PersonalTrainingAudits => Set<PersonalTrainingAudit>();
    public DbSet<FamilyRegistration> FamilyRegistrations => Set<FamilyRegistration>();
    public DbSet<StudentChange> StudentChanges => Set<StudentChange>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<TrainingTeam> TrainingTeams => Set<TrainingTeam>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<TeamChange> TeamChanges => Set<TeamChange>();
    public DbSet<StudentTeamChange> StudentTeamChanges => Set<StudentTeamChange>();
    public DbSet<PlayerFamily> PlayerFamilies => Set<PlayerFamily>();
    public DbSet<FamilyChange> FamilyChanges => Set<FamilyChange>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<FeePlanPrice> FeePlanPrices => Set<FeePlanPrice>();
    public DbSet<StudentFeeAssignment> StudentFeeAssignments => Set<StudentFeeAssignment>();
    public DbSet<StudentTransaction> StudentTransactions => Set<StudentTransaction>();
    public DbSet<MonthlyFeePeriod> MonthlyFeePeriods => Set<MonthlyFeePeriod>();
    public DbSet<StudentMonthlyExemption> StudentMonthlyExemptions => Set<StudentMonthlyExemption>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FamilyPayment> FamilyPayments => Set<FamilyPayment>();
    public DbSet<FamilyTariffAssignment> FamilyTariffAssignments => Set<FamilyTariffAssignment>();
    public DbSet<SizeGroup> SizeGroups => Set<SizeGroup>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockEntry> StockEntries => Set<StockEntry>();
    public DbSet<StockEntryDetail> StockEntryDetails => Set<StockEntryDetail>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TreasuryEntry>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<TreasuryEntry>().HasIndex(x => x.Date);
        builder.Entity<TreasuryEntry>().HasIndex(x => x.Method).IsUnique().HasFilter("\"Kind\" = 'Opening' AND \"CancelledAt\" IS NULL");
        builder.Entity<TreasuryEntry>().HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TreasuryEntry>().HasOne(x => x.CancelledBy).WithMany().HasForeignKey(x => x.CancelledById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FieldPayment>().HasIndex(p => p.RequestId).IsUnique();
        builder.Entity<FieldPayment>().HasOne(p => p.FieldBooking).WithMany(b => b.Payments).HasForeignKey(p => p.FieldBookingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FieldPayment>().HasOne(p => p.Actor).WithMany().HasForeignKey(p => p.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FootballField>().Property(f => f.Revision).IsConcurrencyToken();
        builder.Entity<FootballField>().HasData(
            new FootballField { Id = 1, Name = "Fusha 1", Revision = new Guid("a6111111-1111-4111-8111-111111111111") },
            new FootballField { Id = 2, Name = "Fusha 2", Revision = new Guid("a6222222-2222-4222-8222-222222222222") },
            new FootballField { Id = 3, Name = "Fusha 3", Revision = new Guid("a6333333-3333-4333-8333-333333333333") });
        builder.Entity<TrainingTeam>().HasOne(t => t.FootballField).WithMany().HasForeignKey(t => t.FootballFieldId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FieldBooking>().Property(b => b.Revision).IsConcurrencyToken();
        builder.Entity<FieldBooking>().HasIndex(b => b.RequestId).IsUnique();
        builder.Entity<FieldBooking>().HasIndex(b => b.SeriesId);
        builder.Entity<FieldBooking>().HasIndex(b => new { b.FootballFieldId, b.Date, b.StartsAt });
        builder.Entity<FieldBooking>().HasOne(b => b.FootballField).WithMany().HasForeignKey(b => b.FootballFieldId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FieldBookingChange>().HasOne(c => c.FieldBooking).WithMany(b => b.Changes).HasForeignKey(c => c.FieldBookingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FieldBookingChange>().HasOne(c => c.Actor).WithMany().HasForeignKey(c => c.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FamilyTariffAssignment>().HasIndex(x => new { x.FamilyId, x.EffectiveMonth });
        builder.Entity<FamilyTariffAssignment>().HasOne(x => x.Family).WithMany().HasForeignKey(x => x.FamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FamilyTariffAssignment>().HasOne(x => x.FeePlan).WithMany().HasForeignKey(x => x.FeePlanId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FamilyTariffAssignment>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FamilyPayment>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<FamilyPayment>().HasOne(x => x.Family).WithMany().HasForeignKey(x => x.FamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FamilyPayment>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Payment>().HasOne(x => x.FamilyPayment).WithMany(x => x.Payments).HasForeignKey(x => x.FamilyPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CoachTeam>().HasKey(x=>new{x.UserId,x.TrainingTeamId});
        builder.Entity<CoachTeam>().HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CoachTeam>().HasOne(x=>x.TrainingTeam).WithMany().HasForeignKey(x=>x.TrainingTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FirstMonthFeeChange>().HasIndex(x=>x.RequestId).IsUnique();
        builder.Entity<FirstMonthFeeChange>().HasOne(x=>x.Period).WithMany().HasForeignKey(x=>x.PeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpenseCategory>().Property(x=>x.Revision).IsConcurrencyToken();
        builder.Entity<ExpenseCategory>().HasOne(x=>x.Parent).WithMany().HasForeignKey(x=>x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpenseCategory>().HasIndex(x=>new{x.ParentId,x.Name}).IsUnique();
        builder.Entity<Expense>().HasIndex(x=>x.RequestId).IsUnique();
        builder.Entity<Expense>().HasIndex(x=>x.Date);
        builder.Entity<Expense>().HasOne(x=>x.Category).WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Expense>().HasOne(x=>x.Subcategory).WithMany().HasForeignKey(x=>x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Expense>().HasOne(x=>x.CreatedBy).WithMany().HasForeignKey(x=>x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Expense>().HasOne(x=>x.CancelledBy).WithMany().HasForeignKey(x=>x.CancelledById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpenseCategoryChange>().HasOne(x=>x.ExpenseCategory).WithMany().HasForeignKey(x=>x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PersonalTariff>().Property(x=>x.Revision).IsConcurrencyToken();
        builder.Entity<PersonalTraining>().Property(x=>x.Revision).IsConcurrencyToken();
        builder.Entity<PersonalTraining>().HasIndex(x=>x.RequestId).IsUnique();
        builder.Entity<PersonalCharge>().HasIndex(x=>new{x.PersonalTrainingId,x.Period}).IsUnique();
        builder.Entity<PersonalTraining>().HasOne(x=>x.Student).WithMany().HasForeignKey(x=>x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PersonalTraining>().HasOne(x=>x.PersonalTariff).WithMany().HasForeignKey(x=>x.PersonalTariffId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PersonalCharge>().HasOne(x=>x.PersonalTraining).WithMany(x=>x.Charges).HasForeignKey(x=>x.PersonalTrainingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Payment>().HasOne(x=>x.PersonalCharge).WithMany(x=>x.Payments).HasForeignKey(x=>x.PersonalChargeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Student>().Property(s=>s.Revision).IsConcurrencyToken();
        builder.Entity<FamilyRegistration>().HasIndex(r=>r.RequestId).IsUnique();
        builder.Entity<PlayerFamily>().Property(f=>f.Revision).IsConcurrencyToken();
        builder.Entity<StudentFeeAssignment>().HasOne(a=>a.Family).WithMany().HasForeignKey(a=>a.FamilyId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StudentFeeAssignment>().HasIndex(a=>new{a.FamilyId,a.EffectiveMonth});
        builder.Entity<ProductVariant>().HasIndex(v => new { v.ProductId, v.Size }).IsUnique();
        builder.Entity<ProductVariant>().HasAlternateKey(v => new { v.ProductId, v.Id });
        builder.Entity<ProductVariant>().HasOne(v => v.Product).WithMany(p => p.Variants).HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockEntryDetail>().HasOne(d => d.ProductVariant).WithMany().HasForeignKey(d => new { d.ProductId, d.ProductVariantId }).HasPrincipalKey(v => new { v.ProductId, v.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SaleDetail>().HasOne(d => d.ProductVariant).WithMany().HasForeignKey(d => new { d.ProductId, d.ProductVariantId }).HasPrincipalKey(v => new { v.ProductId, v.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockMovement>().HasOne(d => d.ProductVariant).WithMany().HasForeignKey(d => new { d.ProductId, d.ProductVariantId }).HasPrincipalKey(v => new { v.ProductId, v.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SizeGroup>().HasData(
            new SizeGroup { Id=1, Name="Veshje sportive", Sizes="6, 8, 10, 12, 14, S, M, L, XL, XXL" },
            new SizeGroup { Id=2, Name="Qorape", Sizes="27–30, 31–34, 35–38, 39–42, 43–46" },
            new SizeGroup { Id=3, Name="Pa madhësi", Sizes="Standard" });
        builder.Entity<Student>().HasOne(s=>s.TrainingTeam).WithMany(t=>t.Students).HasForeignKey(s=>s.TrainingTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TrainingTeam>().Property(t=>t.Revision).IsConcurrencyToken();
        builder.Entity<TrainingTeam>().HasIndex(t=>t.IsActive);
        builder.Entity<TrainingSession>().HasIndex(s=>new{s.TrainingTeamId,s.Day,s.StartsAt});
        builder.Entity<FeePlanPrice>().HasIndex(p=>new{p.FeePlanId,p.EffectiveMonth});
        builder.Entity<StudentFeeAssignment>().HasIndex(a=>new{a.StudentId,a.EffectiveMonth});
        builder.Entity<FeePlan>().HasData(new FeePlan{Id=1,Name="Pagesa Mujore"},new FeePlan{Id=2,IsFamily=true,Name="Pagesa Familjare"},new FeePlan{Id=3,Name="Lirim nga Pagesa",IsWaiver=true});
        builder.Entity<FeePlanPrice>().HasData(
            new FeePlanPrice{Id=1,FeePlanId=1,Amount=50,EffectiveMonth=new DateOnly(2000,1,1),Reason="Tarifa fillestare",CreatedAt=new DateTime(2026,9,30,0,0,0,DateTimeKind.Utc)},
            new FeePlanPrice{Id=2,FeePlanId=2,Amount=35,FamilyFirstAmount=40,FamilyAdditionalAmount=35,EffectiveMonth=new DateOnly(2000,1,1),Reason="Tarifa fillestare",CreatedAt=new DateTime(2026,9,30,0,0,0,DateTimeKind.Utc)},
            new FeePlanPrice{Id=3,FeePlanId=3,Amount=0,EffectiveMonth=new DateOnly(2000,1,1),Reason="Lirim nga pagesa",CreatedAt=new DateTime(2026,9,30,0,0,0,DateTimeKind.Utc)});

        builder.Entity<Student>(entity =>
        {
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.LastName);
            entity.HasIndex(x => x.ParentPhone);
            entity.Property(x => x.MonthlyFee).HasColumnType("numeric(18,2)");
        });

        builder.Entity<StudentTransaction>(entity =>
        {
            entity.HasIndex(x => x.StudentId);
            entity.HasIndex(x => x.TransactionDate);
            entity.HasIndex(x => new { x.Year, x.Month });
            entity.HasIndex(x => new { x.StudentId, x.Year, x.Month, x.TransactionType });
            entity.Property(x => x.Debit).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Credit).HasColumnType("numeric(18,2)");
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.HasIndex(x => x.StudentId);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.CategoryId);
            entity.Property(x => x.PurchasePrice).HasColumnType("numeric(18,2)");
            entity.Property(x => x.SalePrice).HasColumnType("numeric(18,2)");
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(x => x.ProductId);
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,2)");
            entity.Property(x => x.PurchasePrice).HasColumnType("numeric(18,2)");
        });

        builder.Entity<Sale>(entity =>
        {
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.HasIndex(x => x.Date);
            entity.Property(x => x.Total).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Discount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.GrandTotal).HasColumnType("numeric(18,2)");
            entity.Property(x => x.AmountPaid).HasColumnType("numeric(18,2)");
        });

        builder.Entity<SaleDetail>(entity =>
        {
            entity.HasIndex(x => x.SaleId);
            entity.Property(x => x.Price).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Discount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Total).HasColumnType("numeric(18,2)");
        });

        builder.Entity<MonthlyFeePeriod>(entity =>
        {
            entity.HasIndex(x => new { x.Year, x.Month });
            entity.HasIndex(x => x.StudentId); 
            entity.HasIndex(x => new { x.StudentId, x.Year, x.Month }).IsUnique();
        });

        builder.Entity<StudentMonthlyExemption>(entity =>
        {
            entity.HasIndex(x => new { x.StudentId, x.Year, x.Month });
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        });

        builder.Entity<StockEntry>(entity =>
        {
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.HasIndex(x => x.DocumentNumber).IsUnique();
            entity.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        });

        builder.Entity<StockEntryDetail>(entity =>
        {
            entity.HasIndex(x => x.StockEntryId);
            entity.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Total).HasColumnType("numeric(18,2)");
        });
    }
}
