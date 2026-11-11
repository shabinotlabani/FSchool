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
  var cancellationInput=Input();var cancelled=await With(db=>new EquipmentOrderService(db).Order(token,cancellationInput));
  try{await With(async db=>{await new EquipmentOrderService(db).CancelByParent(new string('0',64),cancelled,setup.Id);return true;});throw new Exception("Foreign cancellation accepted");}catch(InvalidOperationException){check(true,"An unrelated parent token cannot cancel an equipment order");}
  await With(async db=>{await new EquipmentOrderService(db).CancelByParent(token,cancelled,setup.Id);return true;});
  try{await With(db=>new EquipmentOrderService(db).Order(token,cancellationInput));throw new Exception("Cancelled form replay accepted");}catch(ValidationException){check(true,"Replaying a cancelled order form cannot create or reactivate its charge");}
  await With(async db=>{await new EquipmentOrderService(db).CancelByParent(token,cancelled,setup.Id);return true;});
  try{await With(async db=>{await new EquipmentOrderService(db).CancelByParent(token,id,setup.Id);return true;});throw new Exception("Delivered cancellation accepted");}catch(InvalidOperationException){check(true,"Parents cannot cancel an administrator-confirmed delivery");}
  await With(async db=>{
   check(!(await EquipmentOrderService.Reserved(db)).ContainsKey(setup.Variant)&&(await db.Products.SingleAsync(p=>p.Id==setup.Product)).CurrentStock==2,"Cancellation releases the reservation without altering physical stock");
   check((await new BillingService(db).GetFinancialPositionsAsync(setup.Id))[setup.Id].Debt==50,"Repeated cancellation reverses the order debt only once");return true;
  });
  var onlyOne=await With(async db=>{var p=new Product{Code="ONE-TEST",Name="Only one",SalePrice=25,CurrentStock=1,Variants=[new(){Size="S",CurrentStock=1}]};db.Products.Add(p);await db.SaveChangesAsync();return p.Variants.Single().Id;});
  var unavailable=Input();unavailable.VariantId=onlyOne;
  try{await With(db=>new EquipmentOrderService(db).Order(token,unavailable));throw new Exception("Stock of one accepted");}catch(ValidationException){check(true,"Stock of one is rejected even when submitting directly");}
  async Task<bool> TryLast(){try{await With(db=>new EquipmentOrderService(db).Order(token,Input()));return true;}catch(ValidationException){return false;}}
  var last=await Task.WhenAll(TryLast(),TryLast());check(last.Count(x=>x)==1,"Concurrent parents respect the minimum free stock threshold");
  var otherToken=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
  var pair=await With(async db=>{
   var child=new Student{FirstName="Second",LastName="Equipment",DateOfBirth=new(2015,1,1),ParentLinkToken=otherToken};
   var p=new Product{Code="LAST-PAIR",Name="Last pair",SalePrice=25,CurrentStock=2,Variants=[new(){Size="M",CurrentStock=2}]};db.AddRange(child,p);await db.SaveChangesAsync();return(Child:child.Id,Variant:p.Variants.Single().Id);
  });
  async Task<bool> ReservePair(string parent,int child){try{var request=Input(2);request.StudentId=child;request.VariantId=pair.Variant;await With(db=>new EquipmentOrderService(db).Order(parent,request));return true;}catch(ValidationException){return false;}}
  var competing=await Task.WhenAll(ReservePair(token,setup.Id),ReservePair(otherToken,pair.Child));
  check(competing.Count(x=>x)==1,"Two different parents cannot reserve the same final stock simultaneously");
  var winningOrder=await With(db=>db.SaleDetails.Where(d=>d.ProductVariantId==pair.Variant).Select(d=>d.SaleId).SingleAsync());
  var winningToken=competing[0]?token:otherToken;var winningChild=competing[0]?setup.Id:pair.Child;
  async Task<bool> TryChange(bool cancel){try{await With(async db=>{var service=new EquipmentOrderService(db);if(cancel)await service.CancelByParent(winningToken,winningOrder,winningChild);else await service.Deliver(winningOrder,setup.Actor);return true;});return true;}catch(InvalidOperationException){return false;}}
  var changes=await Task.WhenAll(TryChange(true),TryChange(false));
  await With(async db=>{
   var order=await db.Sales.SingleAsync(s=>s.Id==winningOrder);var variant=await db.ProductVariants.SingleAsync(v=>v.Id==pair.Variant);
   check(changes.Count(x=>x)==1&&(order.Status=="Cancelled"&&variant.CurrentStock==2||order.Status=="Completed"&&variant.CurrentStock==0),"Concurrent parent cancellation and admin delivery leave one consistent stock state");return true;
  });
 }
}
