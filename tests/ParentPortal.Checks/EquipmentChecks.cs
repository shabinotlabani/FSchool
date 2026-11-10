using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

static class EquipmentChecks
{
 public static async Task Run(DbContextOptions<ApplicationDbContext> options,Action<bool,string> check)
 {
  ApplicationDbContext Open()=>new(options,new ClubContext{ClubId=1,IsOwner=true});
  async Task<T> With<T>(Func<ApplicationDbContext,Task<T>> action){await using var db=Open();return await action(db);}
  var token=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
  var setup=await With(async db=>{
   var child=new Student{FirstName="Equipment",LastName="Parent",DateOfBirth=new(2015,1,1),ParentLinkToken=token};
   var product=new Product{Code="EQ-TEST",Name="Test jersey",SalePrice=25,CurrentStock=4,Variants=[new(){Size="M",CurrentStock=4,PurchasePrice=10}]};
   var actor=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="equipment-admin@test",ClubId=1};
   db.AddRange(child,product,actor);await db.SaveChangesAsync();return(child.Id,Product:product.Id,Variant:product.Variants.Single().Id,Actor:actor.Id);
  });
  EquipmentOrderInput Input(int quantity=1)=>new(){StudentId=setup.Id,VariantId=setup.Variant,Quantity=quantity,ExpectedPrice=25,AcceptDebt=true};
  var input=Input(2);
  var id=await With(db=>new EquipmentOrderService(db).Order(token,input));
  check(id==await With(db=>new EquipmentOrderService(db).Order(token,input)),"Repeated parent submissions create one equipment order");
  await With(async db=>{
   check((await db.Products.SingleAsync(p=>p.Id==setup.Product)).CurrentStock==4&&(await EquipmentOrderService.Reserved(db))[setup.Variant]==2,"Parent orders reserve stock without a physical stock exit");
   check((await new BillingService(db).GetFinancialPositionsAsync(setup.Id))[setup.Id].Debt==50,"Equipment orders load the exact debt into the parent's financial account");
   return true;
  });
  try{await With(db=>new EquipmentOrderService(db).Order(token,Input(3)));throw new Exception("Overselling accepted");}catch(ValidationException){check(true,"Reserved quantities cannot be sold by another parent order");}
  try{await With(db=>new IssueService(db).CreateAsync(new(){StudentId=setup.Id,Lines=[new(){ProductId=setup.Product,ProductVariantId=setup.Variant,Quantity=3}]},setup.Actor,false));throw new Exception("Reserved stock issued");}catch(InvalidOperationException){check(true,"Manual stock exits cannot consume quantities reserved for parents");}
  var outsider=Input();outsider.StudentId=int.MaxValue;
  try{await With(db=>new EquipmentOrderService(db).Order(token,outsider));throw new Exception("Foreign child accepted");}catch(ValidationException){check(true,"Parent orders reject a child outside the private link");}
  var price=Input();price.ExpectedPrice=1;
  try{await With(db=>new EquipmentOrderService(db).Order(token,price));throw new Exception("Forged price accepted");}catch(ValidationException){check(true,"Changed or forged order prices are rejected");}
  await Task.WhenAll(With(async db=>{await new EquipmentOrderService(db).Deliver(id,setup.Actor);return true;}),With(async db=>{await new EquipmentOrderService(db).Deliver(id,setup.Actor);return true;}));
  await With(async db=>{
   check((await db.Products.SingleAsync(p=>p.Id==setup.Product)).CurrentStock==2&&await db.StockMovements.CountAsync(m=>m.ReferenceType=="Sale"&&m.ReferenceId==id)==1,"Concurrent delivery confirmation deducts stock only once");
   check((await new BillingService(db).GetFinancialPositionsAsync(setup.Id))[setup.Id].Debt==50&&(await db.Sales.SingleAsync(s=>s.Id==id)).DeliveredAt!=null,"Delivery records its timestamp without charging the parent again");return true;
  });
  var cancelled=await With(db=>new EquipmentOrderService(db).Order(token,Input()));
  await With(async db=>{await new EquipmentOrderService(db).Cancel(cancelled,setup.Actor);return true;});
  await With(async db=>{await new EquipmentOrderService(db).Cancel(cancelled,setup.Actor);return true;});
  await With(async db=>{
   check(!(await EquipmentOrderService.Reserved(db)).ContainsKey(setup.Variant)&&(await db.Products.SingleAsync(p=>p.Id==setup.Product)).CurrentStock==2,"Cancellation releases the reservation without altering physical stock");
   check((await new BillingService(db).GetFinancialPositionsAsync(setup.Id))[setup.Id].Debt==50,"Repeated cancellation reverses the order debt only once");return true;
  });
  var onlyOne=await With(async db=>{var p=new Product{Code="ONE-TEST",Name="Only one",SalePrice=25,CurrentStock=1,Variants=[new(){Size="S",CurrentStock=1}]};db.Products.Add(p);await db.SaveChangesAsync();return p.Variants.Single().Id;});
  var unavailable=Input();unavailable.VariantId=onlyOne;
  try{await With(db=>new EquipmentOrderService(db).Order(token,unavailable));throw new Exception("Stock of one accepted");}catch(ValidationException){check(true,"Stock of one is rejected even when submitting directly");}
  async Task<bool> TryLast(){try{await With(db=>new EquipmentOrderService(db).Order(token,Input()));return true;}catch(ValidationException){return false;}}
  var last=await Task.WhenAll(TryLast(),TryLast());check(last.Count(x=>x)==1,"Concurrent parents respect the minimum free stock threshold");
 }
}
