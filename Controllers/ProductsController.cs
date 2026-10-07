using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Read)]
public class ProductsController(ApplicationDbContext db, InventoryService inventory) : Controller
{
    public async Task<IActionResult> Index(StockFilter filter)
    {
        if(!ModelState.IsValid) return View(new StockOverviewModel { Filter=filter, Categories=await db.Products.AsNoTracking().Select(p=>p.CategoryId).Distinct().OrderBy(c=>c).ToListAsync() });
        return View(await new StockReportService(db).OverviewAsync(filter));
    }

    public async Task<IActionResult> Card(int id, StockFilter filter)
    {
        if(!ModelState.IsValid)
        {
            var product=await db.Products.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==id);
            return product==null?NotFound():View(new StockCardModel {Product=product,Filter=filter});
        }
        var card=await new StockReportService(db).CardAsync(id,filter);
        return card==null?NotFound():View(card);
    }

    public async Task<IActionResult> PrintCard(int id, StockFilter filter)
    {
        if(!ModelState.IsValid)return BadRequest("Kontrolloni periudhën e kartelës.");
        var card=await new StockReportService(db).CardAsync(id,filter);
        return card==null?NotFound():View(card);
    }

    public async Task<IActionResult> PrintStock(StockFilter filter)
    {
        if(!ModelState.IsValid)return BadRequest("Kontrolloni filtrat dhe periudhën e raportit.");
        return View(await new StockReportService(db).OverviewAsync(filter));
    }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Create() => View(new CreateProductModel { SizeGroups = await ActiveGroups() });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Create(CreateProductModel model)
    {
        model.SizeGroups = await ActiveGroups();
        if (!ModelState.IsValid) return View(model);
        int id;
        try { id = await inventory.CreateProductAsync(model); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(model); }
        TempData["Success"] = "Rekuizita u krijua. Regjistroni hyrjen e parë për t'i shtuar stokun.";
        return RedirectToAction(nameof(Receive), new { productId = id });
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Receive(int? productId)
    {
        var model = new CreateStockEntryModel { Products = await ActiveProducts() };
        if (productId.HasValue)
        {
            var product = model.Products.SingleOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();
            model.Lines[0] = new StockEntryLineModel { ProductId = product.Id, Quantity = 1, UnitPrice = product.PurchasePrice };
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Receive(CreateStockEntryModel model)
    {
        model.Products = await ActiveProducts();
        if (!ModelState.IsValid)
        {
            if (model.Lines == null || model.Lines.Count == 0) model.Lines = [new()];
            return View(model);
        }
        try
        {
            var id = await inventory.ReceiveAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"] = "Hyrja u ruajt dhe stoku u përditësua.";
            return RedirectToAction(nameof(Entry), new { id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Hyrja nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri."); return View(model); }
    }

    public async Task<IActionResult> Entries(string? search, DateOnly? from, DateOnly? to, int? productId)
    {
        if (!ModelState.IsValid || from == DateOnly.MinValue || to == DateOnly.MaxValue || to == DateOnly.MinValue || (from.HasValue && to.HasValue && from > to)) return BadRequest("Periudha e kërkimit nuk është e vlefshme.");
        var query = db.StockEntries.AsNoTracking().Include(e => e.CreatedByUser).Include(e => e.Details).AsQueryable();
        if (from.HasValue) { var start = BillingClock.UtcDate(from.Value); query = query.Where(e => e.Date >= start); }
        if (to.HasValue) { var end = BillingClock.UtcDate(to.Value.AddDays(1)); query = query.Where(e => e.Date < end); }
        if (productId.HasValue) query = query.Where(e => e.Details.Any(d => d.ProductId == productId));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(e => EF.Functions.ILike(e.DocumentNumber, pattern) || (e.SupplierName != null && EF.Functions.ILike(e.SupplierName, pattern)) || (e.SupplierInvoiceNo != null && EF.Functions.ILike(e.SupplierInvoiceNo, pattern)));
        }
        return View(new StockEntryListModel { Search = search?.Trim(), From = from, To = to, ProductId = productId, ProductName = productId.HasValue ? await db.Products.Where(p => p.Id == productId).Select(p => p.Name).FirstOrDefaultAsync() : null, Entries = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync() });
    }

    public async Task<IActionResult> Entry(int id)
    {
        var entry = await inventory.GetEntryAsync(id);
        return entry == null ? NotFound() : View(entry);
    }

    public async Task<IActionResult> PrintEntry(int id)
    {
        var entry = await inventory.GetEntryAsync(id);
        return entry == null ? NotFound() : View(entry);
    }

    private Task<List<SizeGroup>> ActiveGroups() => db.SizeGroups.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Id).ToListAsync();

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Variants).SingleOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return View(new EditProductModel { Id = id, ExpectedState = InventoryService.EditState(product), Name = product.Name,
            Category = product.CategoryId, UnitName = product.UnitName, Barcode = product.Barcode, SalePrice = product.SalePrice,
            MinimumStock = product.MinimumStock, IsActive = product.IsActive, Product = product });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Edit(EditProductModel model)
    {
        if (ModelState.IsValid) try
        {
            await inventory.EditProductAsync(model); TempData["Success"] = "Të dhënat e rekuizitës u përditësuan.";
            return RedirectToAction(nameof(Card), new { id = model.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.DataAnnotations.ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Product = await db.Products.AsNoTracking().Include(p => p.Variants).SingleOrDefaultAsync(p => p.Id == model.Id);
        return model.Product == null ? NotFound() : View(model);
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Sizes(int id)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Variants).SingleOrDefaultAsync(p => p.Id == id);
        return product == null ? NotFound() : View(new ProductSizesModel { ProductId = id, Product = product, ActiveIds = product.Variants.Where(v => v.IsActive).Select(v => v.Id).ToList(), Groups = await ActiveGroups() });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Sizes(ProductSizesModel model)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var product = await db.Products.Include(p => p.Variants).SingleOrDefaultAsync(p => p.Id == model.ProductId);
        if (product == null) return NotFound();
        var sizes = SizeGroup.Parse(model.NewSizes);
        if (sizes.Length > 60 || sizes.Any(s => s.Length > 20) || sizes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != sizes.Length)
            ModelState.AddModelError("NewSizes", "Shkruani deri në 60 madhësi të ndryshme, deri në 20 karaktere secila.");
        if (model.ActiveIds.Except(product.Variants.Select(v => v.Id)).Any()) ModelState.AddModelError("", "Madhësia nuk i përket rekuizitës.");
        if (product.Variants.Any(v => !model.ActiveIds.Contains(v.Id) && v.CurrentStock != 0)) ModelState.AddModelError("", "Madhësitë me stok nuk mund të çaktivizohen.");
        if (!ModelState.IsValid) { model.Product = product; model.Groups = await ActiveGroups(); return View(model); }
        foreach (var variant in product.Variants) variant.IsActive = model.ActiveIds.Contains(variant.Id);
        foreach (var size in sizes)
        {
            var existing = product.Variants.SingleOrDefault(v => string.Equals(v.Size, size, StringComparison.OrdinalIgnoreCase));
            if (existing != null) existing.IsActive = true;
            else product.Variants.Add(new ProductVariant { Size = size });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"] = "Madhësitë u ruajtën. Stoku dhe historiku ekzistues janë ruajtur.";
        return RedirectToAction(nameof(Card), new { id = product.Id });
    }

    private Task<List<Product>> ActiveProducts() => db.Products.AsNoTracking().Include(p => p.Variants).Where(p => p.IsActive).OrderBy(p => p.Name).ThenBy(p => p.Size).ToListAsync();
}
