using System.Linq.Expressions;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;

public partial class ApplicationDbContext
{
    public ClubContext Club { get; }
    public int CurrentClubId => Club.ClubId;
    public bool IdentityAccess => Club.IdentityAccess;
    public DbSet<ClubTenant> Clubs => Set<ClubTenant>();

    private void ConfigureClubs(ModelBuilder builder)
    {
        builder.Entity<ClubTenant>().ToTable("Clubs");
        builder.Entity<ClubTenant>().HasData(new ClubTenant { Id = 1, Name = "2 Korriku", IsActive = true,
            CreatedAt = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc) });
        builder.Entity<ApplicationUser>().Property(x => x.ClubId).HasDefaultValue(1);
        builder.Entity<ApplicationUser>().HasOne<ClubTenant>().WithMany().HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ApplicationUser>().HasQueryFilter(x => IdentityAccess || x.ClubId == CurrentClubId || x.IsPlatformOwner);
        var entities = builder.Model.GetEntityTypes().Where(e => e.ClrType.Namespace == typeof(Student).Namespace
            && e.ClrType != typeof(ApplicationUser) && e.ClrType != typeof(ClubTenant)).ToList();
        foreach (var entity in entities)
        {
            var eb = builder.Entity(entity.ClrType);
            eb.Property<int>("ClubId").HasDefaultValue(1).IsConcurrencyToken();
            eb.HasOne(typeof(ClubTenant)).WithMany().HasForeignKey("ClubId").OnDelete(DeleteBehavior.Restrict);
            var parameter = Expression.Parameter(entity.ClrType, "row");
            var clubId = Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(int) }, parameter, Expression.Constant("ClubId"));
            eb.HasQueryFilter(Expression.Lambda(Expression.Equal(clubId, Expression.Property(Expression.Constant(this), nameof(CurrentClubId))), parameter));
            foreach (var index in entity.GetIndexes().Where(x => x.IsUnique).ToList())
            {
                var names = index.Properties.Select(p => p.Name).ToArray();
                // Public parent tokens and idempotency keys remain globally unambiguous.
                if (names.Contains("ParentLinkToken") || names.Contains("RequestId")) continue;
                var filter = index.GetFilter();
                entity.RemoveIndex(index);
                eb.HasIndex(new[] { "ClubId" }.Concat(names).ToArray()).IsUnique().HasFilter(filter);
            }
        }
        // Additional composite constraints enforce isolation even for forged foreign keys.
        foreach (var entity in entities)
        foreach (var fk in entity.GetForeignKeys().Where(f => entities.Contains(f.PrincipalEntityType)).ToList())
        {
            var principal = new[] { "ClubId" }.Concat(fk.PrincipalKey.Properties.Select(p => p.Name)).ToArray();
            var dependent = new[] { "ClubId" }.Concat(fk.Properties.Select(p => p.Name)).ToArray();
            builder.Entity(fk.PrincipalEntityType.ClrType).HasAlternateKey(principal);
            builder.Entity(entity.ClrType).HasOne(fk.PrincipalEntityType.ClrType).WithMany()
                .HasForeignKey(dependent).HasPrincipalKey(principal).OnDelete(DeleteBehavior.Restrict);
        }
    }

    private async Task GuardClubs(CancellationToken ct)
    {
        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        var staffIds = new HashSet<string>();
        foreach (var entry in changes)
        {
            if (entry.Entity is ClubTenant)
            {
                if (!Club.ManagePlatform) throw new InvalidOperationException("Nuk keni qasje në menaxhimin e klubeve.");
                continue;
            }
            if (entry.Entity is ApplicationUser user)
            {
                if (Club.ManagePlatform) continue;
                if (entry.State == EntityState.Added)
                {
                    if (CurrentClubId <= 0 || user.IsPlatformOwner) throw new InvalidOperationException("Zgjidhni klubin.");
                    user.ClubId = CurrentClubId;
                }
                else
                {
                    // Identity marks all scalar fields modified on normal role/password updates.
                    // Compare persisted ownership rather than the IsModified flags.
                    var membership = await Users.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == user.Id)
                        .Select(x => new { x.ClubId, x.IsPlatformOwner }).SingleAsync(ct);
                    if (user.ClubId != membership.ClubId || user.IsPlatformOwner != membership.IsPlatformOwner)
                        throw new InvalidOperationException("Klubi i llogarisë nuk mund të ndryshohet këtu.");
                    if (entry.State == EntityState.Deleted && user.IsPlatformOwner)
                        throw new InvalidOperationException("Llogaria e pronarit të platformës nuk mund të fshihet këtu.");
                    if (!IdentityAccess && !user.IsPlatformOwner && user.ClubId != CurrentClubId)
                        throw new InvalidOperationException("Llogaria nuk i përket këtij klubi.");
                }
                continue;
            }
            if (entry.Metadata.FindProperty("ClubId") == null) continue;
            if (CurrentClubId <= 0) throw new InvalidOperationException("Zgjidhni klubin para se të vazhdoni.");
            var property = entry.Property("ClubId");
            if (entry.State == EntityState.Added) property.CurrentValue = CurrentClubId;
            else if (!Equals(property.OriginalValue, CurrentClubId) || !Equals(property.CurrentValue, CurrentClubId))
                throw new InvalidOperationException("Të dhënat nuk i përkasin këtij klubi.");
            if (entry.State == EntityState.Deleted) continue;
            foreach (var fk in entry.Metadata.GetForeignKeys().Where(f => f.PrincipalEntityType.ClrType == typeof(ApplicationUser)))
            {
                var id = entry.Property(fk.Properties.Single().Name).CurrentValue as string;
                if (id != null) staffIds.Add(id);
            }
        }
        staffIds.ExceptWith(changes.Where(e => e.State == EntityState.Added && e.Entity is ApplicationUser u && u.ClubId == CurrentClubId)
            .Select(e => ((ApplicationUser)e.Entity).Id));
        if (staffIds.Count > 0 && await Users.IgnoreQueryFilters().CountAsync(u => staffIds.Contains(u.Id) && (u.ClubId == CurrentClubId || u.IsPlatformOwner), ct) != staffIds.Count)
            throw new InvalidOperationException("Shfrytëzuesi nuk i përket këtij klubi.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardClubs(default).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await GuardClubs(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
