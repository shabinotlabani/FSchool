using System.ComponentModel.DataAnnotations;
using System.Data;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class StockReportService(ApplicationDbContext db)
{
    private static string Literal(string text)=>text.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_");
    public async Task<StockOverviewModel> OverviewAsync(StockFilter filter)
    {
        Validator.ValidateObject(filter,new ValidationContext(filter),true);
        await using var snapshot=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead):null;
        var model=new StockOverviewModel{Filter=filter,Categories=await db.Products.AsNoTracking().Select(p=>p.CategoryId).Distinct().OrderBy(c=>c).ToListAsync()};
        var query=db.Products.AsNoTracking().Include(p=>p.Variants).AsQueryable();
        if(!string.IsNullOrWhiteSpace(filter.Category))query=query.Where(p=>EF.Functions.ILike(p.CategoryId,Literal(filter.Category),"\\"));
        if(filter.Status=="active")query=query.Where(p=>p.IsActive);
        if(filter.Status=="inactive")query=query.Where(p=>!p.IsActive);
        if(!string.IsNullOrWhiteSpace(filter.Search))
            foreach(var term in filter.Search.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries))
            {
                var pattern="%"+Literal(term)+"%";
                query=query.Where(p=>EF.Functions.ILike(p.Name+" "+p.CategoryId+" "+(p.Size??""),pattern,"\\") || p.Variants.Any(v=>EF.Functions.ILike(v.Size,pattern,"\\")) || (p.Barcode!=null && EF.Functions.ILike(p.Barcode,pattern,"\\")));
            }
        var products=await query.OrderBy(p=>p.Name).ThenBy(p=>p.Size).ThenBy(p=>p.Id).ToListAsync();
        var ids=products.Select(p=>p.Id).ToList();
        var start=BillingClock.UtcDate(filter.From);var end=BillingClock.UtcDate(filter.To.AddDays(1));
        var aggregates=await db.StockMovements.AsNoTracking().Where(m=>ids.Contains(m.ProductId)).GroupBy(m=>m.ProductId)
            .Select(g=>new{Id=g.Key,Total=g.Sum(m=>m.Quantity),Before=g.Sum(m=>m.Date<start?m.Quantity:0),Incoming=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity>0?m.Quantity:0),Outgoing=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity<0?-m.Quantity:0)}).ToDictionaryAsync(g=>g.Id);
        var sizeAggregates=await db.StockMovements.AsNoTracking().Where(m=>ids.Contains(m.ProductId)&&m.ProductVariantId!=null)
            .GroupBy(m=>m.ProductVariantId!.Value)
            .Select(g=>new{Id=g.Key,Total=g.Sum(m=>m.Quantity),Before=g.Sum(m=>m.Date<start?m.Quantity:0),Incoming=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity>0?m.Quantity:0),Outgoing=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity<0?-m.Quantity:0)}).ToDictionaryAsync(g=>g.Id);
        foreach(var product in products)
        {
            aggregates.TryGetValue(product.Id,out var a);
            var undated=product.CurrentStock-(a?.Total??0);
            var row=new StockSummaryRow{Product=product,UndatedStock=undated,Opening=undated+(a?.Before??0),Incoming=a?.Incoming??0,Outgoing=a?.Outgoing??0};
            if(filter.Stock=="empty" && row.Closing>0)continue;
            foreach(var variant in product.Variants.OrderBy(v=>v.Id))
            {
                sizeAggregates.TryGetValue(variant.Id,out var size);
                row.Sizes.Add(new VariantStockRow{Variant=variant,Opening=variant.CurrentStock-(size?.Total??0)+(size?.Before??0),Incoming=size?.Incoming??0,Outgoing=size?.Outgoing??0});
            }
            if(filter.Stock=="low" && !row.IsLow)continue;
            model.Rows.Add(row);
        }
        if(snapshot!=null)await snapshot.CommitAsync();
        return model;
    }

    public async Task<StockCardModel?> CardAsync(int id,StockFilter filter)
    {
        Validator.ValidateObject(filter,new ValidationContext(filter),true);
        await using var snapshot=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead):null;
        var product=await db.Products.AsNoTracking().Include(p=>p.Variants).SingleOrDefaultAsync(p=>p.Id==id);if(product==null)return null;
        var start=BillingClock.UtcDate(filter.From);var end=BillingClock.UtcDate(filter.To.AddDays(1));
        if(filter.VariantId.HasValue && !product.Variants.Any(v=>v.Id==filter.VariantId))return null;
        var all=db.StockMovements.AsNoTracking().Where(m=>m.ProductId==id);
        var sizeTotals=await all.Where(m=>m.ProductVariantId!=null).GroupBy(m=>m.ProductVariantId!.Value)
            .Select(g=>new {Id=g.Key,Total=g.Sum(m=>m.Quantity),Before=g.Sum(m=>m.Date<start?m.Quantity:0),Incoming=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity>0?m.Quantity:0),Outgoing=g.Sum(m=>m.Date>=start&&m.Date<end&&m.Quantity<0?-m.Quantity:0)}).ToDictionaryAsync(g=>g.Id);
        var current=filter.VariantId.HasValue?product.Variants.Single(v=>v.Id==filter.VariantId).CurrentStock:product.CurrentStock;
        if(filter.VariantId.HasValue)all=all.Where(m=>m.ProductVariantId==filter.VariantId);
        var total=await all.SumAsync(m=>m.Quantity);var before=await all.Where(m=>m.Date<start).SumAsync(m=>m.Quantity);
        var model=new StockCardModel{Product=product,Filter=filter,UndatedStock=current-total,Opening=current-total+before};
        foreach(var variant in product.Variants.OrderBy(v=>v.Id))
        {
            sizeTotals.TryGetValue(variant.Id,out var a);
            model.Sizes.Add(new VariantStockRow{Variant=variant,Opening=variant.CurrentStock-(a?.Total??0)+(a?.Before??0),Incoming=a?.Incoming??0,Outgoing=a?.Outgoing??0});
        }
        var movements=await all.Include(m=>m.User).Where(m=>m.Date>=start&&m.Date<end).OrderBy(m=>m.Date).ThenBy(m=>m.CreatedAt).ThenBy(m=>m.Id).ToListAsync();
        var entryIds=movements.Where(m=>m.ReferenceType=="StockEntry"&&m.ReferenceId.HasValue).Select(m=>m.ReferenceId!.Value).Distinct().ToList();
        var saleIds=movements.Where(m=>m.ReferenceType=="Sale"&&m.ReferenceId.HasValue).Select(m=>m.ReferenceId!.Value).Distinct().ToList();
        var entries=await db.StockEntries.AsNoTracking().Where(e=>entryIds.Contains(e.Id)).ToDictionaryAsync(e=>e.Id);
        var sales=await db.Sales.AsNoTracking().Include(s=>s.Student).Where(s=>saleIds.Contains(s.Id)).ToDictionaryAsync(s=>s.Id);
        var balance=model.Opening;
        foreach(var movement in movements)
        {
            balance+=movement.Quantity;
            var row=new StockCardRow{Movement=movement,Balance=balance,Document="Lëvizje #"+movement.Id};
            if(movement.ReferenceType=="StockEntry"&&movement.ReferenceId.HasValue&&entries.TryGetValue(movement.ReferenceId.Value,out var entry))
            {row.Document=entry.DocumentNumber;row.Controller="Products";row.Action="Entry";row.DocumentId=entry.Id;row.Party=entry.SupplierName??"Furnitor i pashënuar";}
            else if(movement.ReferenceType=="Sale"&&movement.ReferenceId.HasValue&&sales.TryGetValue(movement.ReferenceId.Value,out var sale))
            {row.Document=sale.InvoiceNumber;row.Controller="Sales";row.Action="Details";row.DocumentId=sale.Id;row.Party=sale.PlayerName??sale.Student?.FullName??sale.CustomerName??"—";}
            model.Rows.Add(row);
        }
        if(snapshot!=null)await snapshot.CommitAsync();
        return model;
    }
}
