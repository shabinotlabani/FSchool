// Run with NODE_PATH pointing to a Playwright installation.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');const http=require('node:http');
const root=path.resolve(__dirname,'../..');let posts=0,child=1;
const panel=(ordered=false)=>`<div class="parent-equipment-content">${ordered?'<p class="equipment-success" role="status">Faleminderit! Porosia u rezervua.</p>':''}<p class="equipment-intro">Rekuizitat per lojtarin ${child}</p><div class="equipment-scroll"><table class="equipment-table"><thead><tr><th>Rekuizita</th><th>Madhesia</th><th>Cmimi</th><th>E lire</th><th>Porosit</th></tr></thead><tbody><tr><td><strong>Fanelle stervitjeje e klubit</strong></td><td>M</td><td>25.00</td><td>3</td><td><form class="equipment-order-form" action="/order" method="post"><input type="hidden" name="StudentId" value="${child}"/><label>Sasia <input type="number" name="Quantity" value="1" min="1" max="3" required/></label><label><input type="checkbox" name="AcceptDebt" value="true" required/> Pranoj ngarkimin si borxh</label><button>Porosit</button></form></td></tr></tbody></table></div></div>`;
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
  await page.locator('[aria-controls="panel-equipment"]').tap();await page.locator('.equipment-order-form').waitFor();
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  assert.equal(await page.locator('.equipment-scroll').evaluate(e=>e.scrollWidth>e.clientWidth),true);
  await page.locator('.equipment-scroll').evaluate(e=>e.scrollLeft=e.scrollWidth);
  await page.locator('.equipment-order-form button').click();assert.equal(posts,0,'Debt acceptance is required');
  await page.locator('input[type=checkbox]').check();await page.locator('.equipment-order-form button').click();
  await page.locator('.equipment-success').waitFor();assert.equal(posts,1);assert.match(await page.locator('.equipment-success').textContent(),/Faleminderit/);
  assert.deepEqual(errors,[]);await page.screenshot({path:path.join(root,'obj/equipment-mobile.png'),fullPage:true});
  console.log('PASS: Equipment opens in a mobile popup with a horizontally scrolling table.');
  console.log('PASS: Debt consent is required and the successful order displays thanks without leaving the portal.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
