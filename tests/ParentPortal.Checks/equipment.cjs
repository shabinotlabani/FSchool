// Run with NODE_PATH pointing to a Playwright installation.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');const http=require('node:http');
const root=path.resolve(__dirname,'../..');let posts=0,child=1;
const panel=(ordered=false)=>`<div class="parent-equipment-content">${ordered?'<p class="equipment-success" role="status">Faleminderit! Porosia u rezervua.</p>':''}<div class="equipment-welcome"><span>PAJISJET E LOJTARIT</span><h3>Zgjidh per Trim Shabani</h3><p>Prek rekuiziten, zgjidh madhesine dhe porosit.</p></div><div class="equipment-catalog"><details class="equipment-product"><summary><span class="equipment-product-image"><svg viewBox="0 0 48 48" fill="none" stroke="currentColor"><path d="m16 7-11 7 5 10 6-3v21h16V21l6 3 5-10-11-7-8 6Z"/></svg></span><span class="equipment-product-title"><strong>Trenerka e klubit</strong><span>25.00 <small>/ cope</small></span><small>10 madhesi ne dispozicion</small></span><span class="equipment-expand">+</span></summary><form class="equipment-order-form" action="/order" method="post" data-unit-price="25"><input type="hidden" name="StudentId" value="${child}"/><fieldset class="equipment-size-picker"><legend>Zgjidh madhesine</legend><div class="equipment-sizes">${['128','140','152','164','176','XS','S','M','L','XL'].map((size,i)=>`<label class="equipment-size"><input type="radio" name="VariantId" value="${size}" data-available="${i===0?3:2}" required/><span>${size}</span></label>`).join('')}</div></fieldset><div class="equipment-quantity-total"><div><label>Sasia</label><div class="equipment-quantity"><button type="button" data-quantity-step="-1">-</button><input type="number" name="Quantity" value="1" min="1" max="100" required/><button type="button" data-quantity-step="1">+</button></div></div><div class="equipment-total"><span>Gjithsej</span><output>25.00</output></div></div><label class="equipment-consent"><input type="checkbox" name="AcceptDebt" value="true" required/><span>Pranoj qe vlera e porosise te ngarkohet si borxh ne kartelen time.</span></label><button class="equipment-submit">Porosit rekuiziten <span>→</span></button><p class="equipment-order-note">Rekuizita rezervohet per ty. Dorezimi konfirmohet nga klubi.</p></form></details></div><details class="equipment-history"><summary>Porosite e mia <span>1</span></summary><table class="equipment-history-table"><thead><tr><th>Porosia</th><th>Statusi</th><th>Vlera</th></tr></thead><tbody><tr><td><strong>Trenerka e klubit</strong><small>128 - 1 cope</small></td><td>E rezervuar</td><td>25.00</td></tr></tbody></table></details></div>`;
const server=http.createServer(async(req,res)=>{
 if(req.url.startsWith('/js/')||req.url.startsWith('/css/')){res.setHeader('Content-Type',req.url.startsWith('/js/')?'text/javascript':'text/css');return res.end(fs.readFileSync(path.join(root,'wwwroot',req.url)));}
 if(req.url==='/equipment'){res.setHeader('Content-Type','text/html');return res.end(panel());}
 if(req.url==='/order'){posts++;await new Promise(r=>setTimeout(r,180));res.setHeader('Content-Type','text/html');return res.end(panel(true));}
 res.setHeader('Content-Type','text/html');res.end(`<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/css/parent-portal.css"><link rel="stylesheet" href="/css/player-passport.css"><link rel="stylesheet" href="/css/parent-polish.css"><link rel="stylesheet" href="/css/parent-club-theme.css"><link rel="stylesheet" href="/css/parent-equipment.css"><main class="portal"><div class="portal-actions"><details id="equipment"><summary><span>Rekuizitat</span><small>Porosit pajisjet</small></summary><div class="parent-care-content" data-parent-equipment-url="/equipment"><p>Duke ngarkuar</p></div></details></div></main><script src="/js/parent-equipment.js"></script><script src="/js/parent-panels.js"></script>`);
});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:390,height:844},isMobile:true,hasTouch:true});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port);assert.equal(posts,0);
  await page.locator('[aria-controls="panel-equipment"]').tap();await page.locator('.equipment-product>summary').waitFor();
  assert.equal(await page.locator('.equipment-order-form').isVisible(),false);
  await page.locator('.equipment-product>summary').tap();
  await page.locator('.equipment-submit').click();assert.equal(posts,0,'Size and debt acceptance are required');
  await page.locator('[name=VariantId][value="128"]').check();
  await page.locator('[data-quantity-step="1"]').click();await page.locator('[data-quantity-step="1"]').click();
  assert.equal(await page.locator('[name=Quantity]').inputValue(),'3');
  assert.match(await page.locator('output').textContent(),/75/);
  await page.locator('[name=VariantId][value="140"]').check();
  assert.equal(await page.locator('[name=Quantity]').inputValue(),'2','Selecting a size clamps quantity to its free stock');
  assert.match(await page.locator('output').textContent(),/50/);
  assert.equal(await page.locator('[data-quantity-step="1"]').isDisabled(),true);
  await page.locator('.equipment-history>summary').click();
  for(const width of [320,360,390]){
   await page.setViewportSize({width,height:844});
   assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
   assert.equal(await page.locator('#panel-equipment').evaluate(e=>e.scrollWidth>e.clientWidth),false,'No horizontal scroll in the popup');
  }
  await page.screenshot({path:path.join(root,'obj/equipment-mobile.png'),fullPage:true});
  await page.locator('.equipment-submit').click();assert.equal(posts,0,'Debt acceptance is required');
  await page.locator('input[type=checkbox]').check();await page.locator('.equipment-submit').click();
  await page.locator('.equipment-success').waitFor();assert.equal(posts,1);assert.match(await page.locator('.equipment-success').textContent(),/Faleminderit/);
  assert.deepEqual(errors,[]);
  console.log('PASS: Product tap expands size buttons, quantity and live total without horizontal scrolling.');
  console.log('PASS: Debt consent is required and the successful order displays thanks without leaving the portal.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
