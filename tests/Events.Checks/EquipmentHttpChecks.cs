using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

static class EquipmentHttpChecks
{
 public static async Task Run(HttpClient client,DbContextOptions<ApplicationDbContext> options,int player,string token,string root,Action<bool,string> check)
 {
  ApplicationDbContext Db()=>new(options,new ClubContext{ClubId=1,IsOwner=true});
  int productId,variantId;var image=await File.ReadAllBytesAsync(Path.Combine(root,"wwwroot/images/upnext/logo_256.webp"));
  await using(var db=Db()){
   var product=new Product{Code="HTTP-EQ",Name="HTTP jersey",SalePrice=20,CurrentStock=3,Variants=[new(){Size="M",CurrentStock=2},new(){Size="ONE-HIDDEN",CurrentStock=1},new(){Size="ZERO-HIDDEN",CurrentStock=0}]};
   db.Products.Add(product);await db.SaveChangesAsync();productId=product.Id;variantId=product.Variants.First().Id;
  }
  var edit=await client.GetStringAsync("/Products/Edit/"+productId);
  string Csrf(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
  using(var upload=new MultipartFormDataContent()){
   upload.Add(new StringContent(Csrf(edit)),"__RequestVerificationToken");upload.Add(new ByteArrayContent(image),"photo","jersey.webp");
   check((await client.PostAsync("/Products/SavePhoto/"+productId,upload)).IsSuccessStatusCode,"Administrator can upload an optimized equipment photograph");
  }
  var photo=await client.GetAsync($"/p/{token}/equipment/{productId}/photo");
  check(photo.IsSuccessStatusCode&&(await photo.Content.ReadAsByteArrayAsync()).SequenceEqual(image),"Private parent equipment photo route serves the saved image");
  var path=$"/p/{token}/equipment?studentId={player}";
  var full=await client.GetStringAsync(path);var panel=await client.GetStringAsync(path+"&panel=true");
  check(!panel.Contains("ONE-HIDDEN")&&!panel.Contains("ZERO-HIDDEN"),"Parent catalogue hides sizes with zero or one free unit");
  check(full.Contains("<html")&&panel.Contains("HTTP jersey")&&!panel.Contains("<html"),"Equipment renders a full page or a popup fragment as requested");
  check((await client.PostAsync($"/p/{token}/equipment/order",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Equipment orders require antiforgery protection");
  var request=Regex.Match(panel,"name=\"RequestId\" value=\"([^\"]+)\"").Groups[1].Value;
  var fields=new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(panel)},{"StudentId",player.ToString()},{"VariantId",variantId.ToString()},{"Quantity","1"},{"ExpectedPrice","20.00"},{"AcceptDebt","true"},{"RequestId",request},{"panel","true"}};
  var post=await client.PostAsync($"/p/{token}/equipment/order",new FormUrlEncodedContent(fields));
  check(post.StatusCode==HttpStatusCode.Redirect,"The real parent equipment form creates a reservation");
  var thanks=await client.GetStringAsync(post.Headers.Location!);
  check(thanks.Contains("Faleminderit")&&thanks.Contains("HTTP jersey")&&!thanks.Contains("<html"),"Order confirmation thanks the parent inside the equipment popup");
  check(!thanks.Contains("equipment-product\""),"Reserved stock of one disappears from the product selector");
  int orderId;await using(var db=Db()){orderId=await db.Sales.Where(s=>s.RequestId==Guid.Parse(request)).Select(s=>s.Id).SingleAsync();}
  check((await client.GetAsync("/Sales/Print/"+orderId)).StatusCode==HttpStatusCode.BadRequest,"A reserved order cannot print as a delivered stock exit");
  foreach(var url in new[]{"/Products/Stock","/Products/Card/"+productId,"/Sales/Orders","/Sales/Details/"+orderId,"/Payments/Account/"+player}){
   var response=await client.GetAsync(url);if(!response.IsSuccessStatusCode)throw new Exception("Equipment page render failed: "+url+" "+await response.Content.ReadAsStringAsync());
   check(true,"MVC renders equipment workflow "+url);
  }
  var orders=await client.GetStringAsync("/Sales/Orders");
  var delivered=await client.PostAsync("/Sales/Deliver/"+orderId,new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(orders)}}));
  check(delivered.StatusCode==HttpStatusCode.Redirect&&delivered.Headers.Location!.ToString().Contains("autoPrint=True",StringComparison.OrdinalIgnoreCase),"Admin delivery confirmation opens automatic printing");
  var printed=await client.GetStringAsync(delivered.Headers.Location!);
  check(printed.Contains("HTTP jersey")&&printed.Contains("issue-print.js")&&printed.Contains("KOPJA"),"Confirmed equipment renders the existing two-copy handover document");
 }
}
