using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Services;

public class EquipmentOrderService(ApplicationDbContext db)
{
    public static Task<Dictionary<int,decimal>> Reserved(ApplicationDbContext db) => db.SaleDetails.AsNoTracking()
        .Where(d=>d.Sale!.IsParentOrder&&d.Sale.Status=="Reserved"&&d.ProductVariantId!=null)
        .GroupBy(d=>d.ProductVariantId!.Value).ToDictionaryAsync(g=>g.Key,g=>g.Sum(d=>d.Quantity));

    public async Task<int> Order(string token,EquipmentOrderInput input)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        if(input.RequestId==Guid.Empty||!input.AcceptDebt)throw new ValidationException("Pranoni ngarkimin e vlerës si borxh.");
        await using var tx=await db.Database.BeginTransactionAsync();
        // Same lock order as IssueService: serialize debt changes, then stock changes.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var access=await new ParentAccessService(db).Resolve(token);
        var child=access?.Students.SingleOrDefault(s=>s.Id==input.StudentId&&s.IsActive);
        if(child==null)throw new ValidationException("Lojtari nuk është i vlefshëm për këtë portal.");
        var existing=await db.Sales.AsNoTracking().Include(s=>s.Details).SingleOrDefaultAsync(s=>s.RequestId==input.RequestId);
        if(existing!=null){
            if(!existing.IsParentOrder||existing.StudentId!=child.Id||existing.Details.Count!=1||existing.Details.Single().ProductVariantId!=input.VariantId||existing.Details.Single().Quantity!=input.Quantity||existing.Details.Single().Price!=input.ExpectedPrice)
                throw new ValidationException("Formulari është përdorur. Rihapni rekuizitat.");
            return existing.Id;
        }
        var variant=await db.ProductVariants.AsNoTracking().Include(v=>v.Product).SingleOrDefaultAsync(v=>v.Id==input.VariantId&&v.IsActive&&v.Product!.IsActive);
        if(variant==null)throw new ValidationException("Madhësia nuk është më në dispozicion.");
        var product=variant.Product!;
        if(product.SalePrice<=0||product.SalePrice!=input.ExpectedPrice)throw new ValidationException("Çmimi ka ndryshuar. Rihapni rekuizitat para porosisë.");
        var reserved=await Reserved(db);
        var productReserved=await db.SaleDetails.Where(d=>d.ProductId==product.Id&&d.Sale!.IsParentOrder&&d.Sale.Status=="Reserved").SumAsync(d=>(decimal?)d.Quantity)??0;
        if(variant.CurrentStock-reserved.GetValueOrDefault(variant.Id)<input.Quantity||product.CurrentStock-productReserved<input.Quantity)
            throw new ValidationException("Sasia nuk është më në dispozicion. Zgjidhni një sasi tjetër.");
        var total=decimal.Round(product.SalePrice*input.Quantity,2,MidpointRounding.AwayFromZero);
        var sale=new Sale{RequestId=input.RequestId,IsParentOrder=true,Status="Reserved",StudentId=child.Id,PlayerName=child.FullName,ParentName=child.ParentName,
            InvoiceNumber="POR-"+Guid.NewGuid().ToString("N"),Total=total,GrandTotal=total,Notes="Porosi nga portali i prindit"};
        sale.Details.Add(new(){ProductId=product.Id,ProductVariantId=variant.Id,ProductName=product.Name,ProductSize=variant.Size,UnitName=product.UnitName,Quantity=input.Quantity,Price=product.SalePrice,ListPrice=product.SalePrice,Total=total});
        db.Sales.Add(sale);await db.SaveChangesAsync();sale.InvoiceNumber=$"POR-{BillingClock.Today.Year}-{sale.Id:000000}";
        db.StudentTransactions.Add(new(){StudentId=child.Id,TransactionType="Sale",Description="Porosi rekuizitash "+sale.InvoiceNumber,Debit=total,ReferenceType="Sale",ReferenceId=sale.Id,Year=BillingClock.Today.Year,Month=BillingClock.Today.Month});
        await db.SaveChangesAsync();await tx.CommitAsync();return sale.Id;
    }

    public async Task Deliver(int id,string actor)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var sale=await db.Sales.Include(s=>s.Details).SingleOrDefaultAsync(s=>s.Id==id&&s.IsParentOrder)??throw new InvalidOperationException("Porosia nuk u gjet.");
        await db.Entry(sale).ReloadAsync();
        if(sale.Status=="Completed")return;
        if(sale.Status!="Reserved")throw new InvalidOperationException("Porosia nuk është në pritje të dorëzimit.");
        foreach(var detail in sale.Details){
            var variant=await db.ProductVariants.AsNoTracking().SingleAsync(v=>v.Id==detail.ProductVariantId);
            var product=await db.Products.AsNoTracking().SingleAsync(p=>p.Id==detail.ProductId);
            if(variant.CurrentStock<detail.Quantity||product.CurrentStock<detail.Quantity)throw new InvalidOperationException("Stoku fizik nuk mjafton për dorëzimin.");
            await db.Products.Where(p=>p.Id==product.Id).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.CurrentStock,p=>p.CurrentStock-detail.Quantity).SetProperty(p=>p.UpdatedAt,DateTime.UtcNow));
            await db.ProductVariants.Where(v=>v.Id==variant.Id).ExecuteUpdateAsync(s=>s.SetProperty(v=>v.CurrentStock,v=>v.CurrentStock-detail.Quantity));
            db.StockMovements.Add(new(){ProductId=product.Id,ProductVariantId=variant.Id,ProductSize=detail.ProductSize,UnitName=detail.UnitName,Quantity=-detail.Quantity,PurchasePrice=variant.PurchasePrice,MovementType="Sale",ReferenceType="Sale",ReferenceId=sale.Id,UserId=actor,Notes=sale.InvoiceNumber});
        }
        sale.Status="Completed";sale.DeliveredAt=DateTime.UtcNow;sale.DeliveredById=actor;
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Cancel(int id,string actor)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var sale=await db.Sales.SingleOrDefaultAsync(s=>s.Id==id&&s.IsParentOrder)??throw new InvalidOperationException("Porosia nuk u gjet.");
        await db.Entry(sale).ReloadAsync();if(sale.Status=="Cancelled")return;
        if(sale.Status!="Reserved"||sale.AmountPaid!=0||await db.PendingBankPayments.AnyAsync(p=>p.Kind=="Sale"&&p.TargetId==sale.Id&&p.ReviewedAt==null))throw new InvalidOperationException("Anulohen vetëm porositë e padorëzuara dhe pa pagesa në pritje.");
        sale.Status="Cancelled";sale.Discount=sale.Total;sale.GrandTotal=0;
        db.StudentTransactions.Add(new(){StudentId=sale.StudentId!.Value,TransactionType="SaleCancellation",Description="Anulim porosie "+sale.InvoiceNumber,Credit=sale.Total,ReferenceType="Sale",ReferenceId=sale.Id,CreatedByUserId=actor,Year=BillingClock.Today.Year,Month=BillingClock.Today.Month});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
