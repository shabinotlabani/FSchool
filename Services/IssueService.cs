using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class IssueService(ApplicationDbContext db)
{
    public async Task<int> CreateAsync(CreateIssueModel input, string actorId, bool canWaive)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        foreach (var line in input.Lines)
        {
            Validator.ValidateObject(line, new ValidationContext(line), true);
            if (line.UnitPrice.HasValue && decimal.Round(line.UnitPrice.Value, 2) != line.UnitPrice.Value) throw new InvalidOperationException("Çmimi lejon vetëm dy shifra dhjetore.");
            if (decimal.Round(line.Quantity, 2) != line.Quantity) throw new InvalidOperationException("Sasia lejon dy shifra dhjetore.");
        }
        if (input.WaivePayment && !canWaive) throw new InvalidOperationException("Vetëm administratori mund të lirojë nga pagesa.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var existing = await db.Sales.AsNoTracking().Include(s => s.Details).Include(s => s.Payments).SingleOrDefaultAsync(s => s.RequestId == input.RequestId);
        if (existing != null)
        {
            var originalPrices = existing.Details.GroupBy(d => d.ProductId).ToDictionary(g => g.Key, g => g.First().ListPrice ?? g.First().Price);
            var initialPayment = existing.Payments.SingleOrDefault(p => p.RequestId == input.RequestId)?.Amount ?? 0;
            if (existing.StudentId != input.StudentId || existing.UserId != actorId || initialPayment != input.AmountPaid || existing.PaymentMethod != input.PaymentMethod
                || (existing.WaiverReason != null) != input.WaivePayment || existing.WaiverReason != (input.WaivePayment ? input.WaiverReason?.Trim() : null) || existing.Notes != input.Notes?.Trim()
                || existing.Details.Count != input.Lines.Count || !existing.Details.Select(d => (d.ProductId, d.ProductVariantId, d.Quantity, d.Price)).Order().SequenceEqual(
                    input.Lines.Select(l => (l.ProductId, (int?)l.ProductVariantId, l.Quantity, l.UnitPrice ?? originalPrices.GetValueOrDefault(l.ProductId))).Order()))
                throw new InvalidOperationException("Ky formular është ruajtur. Hapni një dalje të re.");
            return existing.Id;
        }
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == input.StudentId && s.IsActive) ?? throw new InvalidOperationException("Zgjidhni një lojtar aktiv.");
        var ids = input.Lines.Select(l => l.ProductId).ToList();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id) && p.IsActive).ToDictionaryAsync(p => p.Id);
        var variantIds = input.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variants = await db.ProductVariants.AsNoTracking().Where(v => variantIds.Contains(v.Id) && v.IsActive).ToDictionaryAsync(v => v.Id);
        foreach (var group in input.Lines.GroupBy(l => l.ProductVariantId))
        {
            if (!variants.TryGetValue(group.Key, out var variant) || group.Any(l => l.ProductId != variant.ProductId))
                throw new InvalidOperationException("Zgjidhni një madhësi aktive të rekuizitës për Çdo rresht.");
            if (variant.CurrentStock < group.Sum(l => l.Quantity))
                throw new InvalidOperationException($"Stoku për madhësinë {variant.Size} nuk mjafton për sasinë totale të rreshtave.");
        }
        foreach (var group in input.Lines.GroupBy(l => l.ProductId))
            if (!products.TryGetValue(group.Key, out var product) || product.CurrentStock < group.Sum(l => l.Quantity))
                throw new InvalidOperationException("Stoku i rekuizitës nuk mjafton. Kontrolloni sasitë.");
        var sale = new Sale { RequestId = input.RequestId, InvoiceNumber = "DAL-" + Guid.NewGuid().ToString("N"), StudentId = student.Id, PlayerName = student.FullName, ParentName = student.ParentName, UserId = actorId, PaymentMethod = input.PaymentMethod, Notes = input.Notes?.Trim(), WaiverReason = input.WaivePayment ? input.WaiverReason!.Trim() : null };
        foreach (var line in input.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var p) || p.CurrentStock < line.Quantity) throw new InvalidOperationException("Stoku nuk mjafton ose rekuizita nuk është aktive. Kontrolloni sasitë.");
            if (p.SalePrice < 0) throw new InvalidOperationException("Çmimi i rekuizitës nuk është i vlefshëm.");
            var price = line.UnitPrice ?? p.SalePrice;
            if (price == 0 && !input.WaivePayment) throw new InvalidOperationException("Për çmim zero përdorni lirimin me arsye dhe shënim.");
            sale.Details.Add(new SaleDetail { ProductId = p.Id, ProductName = p.Name, ProductSize = variants[line.ProductVariantId].Size, ProductVariantId = line.ProductVariantId, UnitName = p.UnitName, Quantity = line.Quantity, Price = price, ListPrice = p.SalePrice, Total = decimal.Round(price * line.Quantity, 2, MidpointRounding.AwayFromZero) });
        }
        sale.Total = sale.Details.Sum(d => d.Total);
        if (sale.Total == 0 && !input.WaivePayment) throw new InvalidOperationException("Për dalje me vlerë zero zgjidhni lirimin dhe shkruani arsyen e shënimin.");
        sale.Discount = input.WaivePayment ? sale.Total : 0;
        sale.GrandTotal = sale.Total - sale.Discount;
        if (input.AmountPaid > sale.GrandTotal) throw new InvalidOperationException("Pagesa nuk mund të tejkalojë vlerën e daljes.");
        sale.Status = input.WaivePayment ? "Waived" : "Completed";
        db.Sales.Add(sale);
        await db.SaveChangesAsync();
        sale.InvoiceNumber = $"DAL-{BillingClock.Today.Year}-{sale.Id:000000}";
        foreach (var detail in sale.Details)
        {
            var p = products[detail.ProductId];
            await db.Products.Where(x => x.Id == p.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.CurrentStock, v => v.CurrentStock - detail.Quantity).SetProperty(v => v.UpdatedAt, DateTime.UtcNow));
            await db.ProductVariants.Where(v => v.Id == detail.ProductVariantId).ExecuteUpdateAsync(x => x.SetProperty(v => v.CurrentStock, v => v.CurrentStock - detail.Quantity));
            db.StockMovements.Add(new StockMovement { ProductId = p.Id, ProductVariantId = detail.ProductVariantId, ProductSize = detail.ProductSize, UnitName = detail.UnitName, Quantity = -detail.Quantity, PurchasePrice = variants[detail.ProductVariantId!.Value].PurchasePrice, MovementType = "Sale", ReferenceType = "Sale", ReferenceId = sale.Id, UserId = actorId, Notes = sale.InvoiceNumber });
        }
        db.StudentTransactions.Add(Entry(sale, "Sale", sale.Total, 0, actorId, "Dalje rekuizitash " + sale.InvoiceNumber));
        if (input.WaivePayment) db.StudentTransactions.Add(Entry(sale, "SaleWaiver", 0, sale.Discount, actorId, "Lirim: " + sale.WaiverReason));
        if (input.AmountPaid > 0) await AddPayment(sale, input.AmountPaid, input.PaymentMethod, input.RequestId, null, actorId);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return sale.Id;
    }

    public async Task<int> PayAsync(IssuePaymentModel input, string actorId)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        var duplicate = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.RequestId == input.RequestId);
        if (duplicate != null)
        {
            if (duplicate.SaleId != input.SaleId || duplicate.Amount != input.Amount || duplicate.CreatedByUserId != actorId || duplicate.PaymentMethod != input.PaymentMethod || duplicate.Notes != input.Notes?.Trim()) throw new InvalidOperationException("Ky formular është përdorur. Hapni një pagesë të re.");
            return duplicate.Id;
        }
        var sale = await db.Sales.SingleOrDefaultAsync(s => s.Id == input.SaleId && s.RequestId != null) ?? throw new InvalidOperationException("Dalja nuk u gjet.");
        // Reload under the lock even if this context previously read the sale.
        await db.Entry(sale).ReloadAsync();
        if (input.Amount > sale.Due) throw new InvalidOperationException("Shuma tejkalon borxhin e mbetur.");
        var payment = await AddPayment(sale, input.Amount, input.PaymentMethod, input.RequestId, input.Notes?.Trim(), actorId);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return payment.Id;
    }

    private async Task<Payment> AddPayment(Sale sale, decimal amount, string method, Guid request, string? notes, string actor)
    {
        var payment = new Payment { SaleId = sale.Id, StudentId = sale.StudentId!.Value, Amount = amount, RequestId = request, PaymentMethod = method, CreatedByUserId = actor, Notes = notes };
        db.Payments.Add(payment);
        sale.AmountPaid += amount;
        await db.SaveChangesAsync();
        var entry = Entry(sale, "SalePayment", 0, amount, actor, "Arkëtim për " + sale.InvoiceNumber);
        entry.ReferenceId = payment.Id;
        entry.ReferenceType = "SalePayment";
        db.StudentTransactions.Add(entry);
        return payment;
    }

    private static StudentTransaction Entry(Sale sale, string type, decimal debit, decimal credit, string actor, string description) => new() { StudentId = sale.StudentId!.Value, Year = BillingClock.Today.Year, Month = BillingClock.Today.Month, TransactionType = type, Debit = debit, Credit = credit, ReferenceType = "Sale", ReferenceId = sale.Id, CreatedByUserId = actor, Description = description };
}
