using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class InventoryService(ApplicationDbContext db)
{
    public const string LockSql = "SELECT pg_advisory_xact_lock(20260929, 2003)";

    public static string EditState(Product product) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { product.Id, product.Name, product.CategoryId, product.UnitName, product.Barcode, product.SalePrice, product.MinimumStock, product.IsActive }))));

    public async Task EditProductAsync(EditProductModel input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(LockSql);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == input.Id) ?? throw new InvalidOperationException("Rekuizita nuk u gjet.");
        await db.Entry(product).ReloadAsync();
        if (EditState(product) != input.ExpectedState) throw new InvalidOperationException("Të dhënat kanë ndryshuar ndërkohë. Rihapni formularin para ruajtjes.");
        product.Name = input.Name.Trim(); product.CategoryId = input.Category.Trim(); product.UnitName = input.UnitName;
        product.Barcode = Clean(input.Barcode); product.SalePrice = input.SalePrice; product.MinimumStock = input.MinimumStock;
        product.IsActive = input.IsActive; product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<int> CreateProductAsync(CreateProductModel input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var group = await db.SizeGroups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == input.SizeGroupId && g.IsActive)
            ?? throw new InvalidOperationException("Zgjidhni një grup aktiv të madhësive.");
        var product = new Product { Name = input.Name.Trim(), CategoryId = input.Category.Trim(), UnitName = input.UnitName, Barcode = Clean(input.Barcode), SalePrice = input.SalePrice, MinimumStock = input.MinimumStock, Code = "ART-" + Guid.NewGuid().ToString("N") };
        foreach (var size in SizeGroup.Parse(group.Sizes)) product.Variants.Add(new ProductVariant { Size = size });
        db.Products.Add(product);
        await db.SaveChangesAsync();
        product.Code = $"ART-{product.Id:000000}";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return product.Id;
    }

    public async Task<int> ReceiveAsync(CreateStockEntryModel input, string actorId)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        foreach (var line in input.Lines) Validator.ValidateObject(line, new ValidationContext(line), true);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(LockSql);
        var existing = await db.StockEntries.Include(e => e.Details).SingleOrDefaultAsync(e => e.RequestId == input.RequestId);
        if (existing != null)
        {
            if (existing.CreatedByUserId != actorId || existing.Date != BillingClock.UtcDate(input.Date!.Value)
                || existing.SupplierName != Clean(input.SupplierName) || existing.SupplierInvoiceNo != Clean(input.SupplierInvoiceNo)
                || existing.Notes != Clean(input.Notes) || existing.Details.Count != input.Lines.Count
                || !existing.Details.Select(d => (d.ProductId, d.ProductVariantId, d.Quantity, d.UnitPrice)).Order().SequenceEqual(
                    input.Lines.Select(l => (l.ProductId!.Value, l.ProductVariantId, l.Quantity!.Value, l.UnitPrice!.Value)).Order()))
                throw new InvalidOperationException("Ky formular është përdorur. Hapni hyrje të re për sasi të tjera.");
            await transaction.CommitAsync();
            return existing.Id;
        }
        var ids = input.Lines.Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id) && p.IsActive).ToDictionaryAsync(p => p.Id);
        if (products.Count != ids.Count) throw new InvalidOperationException("Një nga rekuizitat nuk ekziston ose është joaktive.");
        var variantIds = input.Lines.Select(l => l.ProductVariantId!.Value).Distinct().ToList();
        var variants = await db.ProductVariants.AsNoTracking().Where(v => variantIds.Contains(v.Id) && v.IsActive).ToDictionaryAsync(v => v.Id);
        var entry = new StockEntry { RequestId = input.RequestId, DocumentNumber = "HYR-" + Guid.NewGuid().ToString("N"), Date = BillingClock.UtcDate(input.Date!.Value), SupplierName = Clean(input.SupplierName), SupplierInvoiceNo = Clean(input.SupplierInvoiceNo), Notes = Clean(input.Notes), CreatedByUserId = actorId };
        foreach (var line in input.Lines)
        {
            var product = products[line.ProductId!.Value];
            if (!variants.TryGetValue(line.ProductVariantId!.Value, out var variant) || variant.ProductId != product.Id)
                throw new InvalidOperationException("Zgjidhni një madhësi aktive të rekuizitës për Çdo rresht.");
            var quantity = line.Quantity!.Value;
            var unitPrice = line.UnitPrice!.Value;
            entry.Details.Add(new StockEntryDetail { ProductId = product.Id, ProductName = product.Name, ProductCode = product.Code, ProductSize = variant.Size, ProductVariantId = variant.Id, UnitName = product.UnitName, Quantity = quantity, UnitPrice = unitPrice, Total = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero) });
        }
        entry.TotalAmount = entry.Details.Sum(d => d.Total);
        db.StockEntries.Add(entry);
        await db.SaveChangesAsync();
        entry.DocumentNumber = $"HYR-{input.Date.Value.Year}-{entry.Id:000000}";
        foreach (var detail in entry.Details)
        {
            // Atomic increments also protect against stale product entities in another request.
            var now = DateTime.UtcNow;
            await db.Products.Where(p => p.Id == detail.ProductId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CurrentStock, p => p.CurrentStock + detail.Quantity)
                .SetProperty(p => p.PurchasePrice, detail.UnitPrice)
                .SetProperty(p => p.UpdatedAt, now));
            await db.ProductVariants.Where(v => v.Id == detail.ProductVariantId).ExecuteUpdateAsync(s => s
                .SetProperty(v => v.CurrentStock, v => v.CurrentStock + detail.Quantity).SetProperty(v => v.PurchasePrice, detail.UnitPrice));
            db.StockMovements.Add(new StockMovement { ProductId = detail.ProductId, ProductVariantId = detail.ProductVariantId, ProductSize = detail.ProductSize, UnitName = detail.UnitName, Date = entry.Date, MovementType = "StockEntry", Quantity = detail.Quantity, PurchasePrice = detail.UnitPrice, ReferenceType = "StockEntry", ReferenceId = entry.Id, UserId = actorId, Notes = $"Hyrje {entry.DocumentNumber}" });
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return entry.Id;
    }

    public Task<StockEntry?> GetEntryAsync(int id) => db.StockEntries.AsNoTracking().Include(e => e.CreatedByUser).Include(e => e.Details).ThenInclude(d => d.Product).SingleOrDefaultAsync(e => e.Id == id);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
