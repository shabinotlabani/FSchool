// NODE_PATH must include the existing Playwright tools directory.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const root = path.resolve(__dirname, '../..');
let delay = 0, fail = false;
const profile = id => `<div id="parent-athlete-content" data-student-id="${id}" data-student-name="Child ${id}" data-reopen-url="/p/test?studentId=${id}"><div class="swipe-surface"><h2>Child ${id}</h2></div><div class="portal-actions"><details id="attendance"><summary><span>Training</span></summary><div class="parent-care-content">Training ${id}</div></details><details id="events"><summary><span>Events</span><small></small></summary><div class="parent-care-content" data-parent-events-url="/events?studentId=${id}"></div></details></div></div>`;
const server = http.createServer(async (req, res) => {
 const url = new URL(req.url, 'http://localhost');
 if (url.pathname.startsWith('/js/')) {
  res.setHeader('Content-Type', 'application/javascript');
  return res.end(fs.readFileSync(path.join(root, 'wwwroot', url.pathname)));
 }
 res.setHeader('Content-Type', 'text/html');
 if (url.pathname === '/events') return res.end(`<div class="parent-event-content" data-event-pending="0">Events ${url.searchParams.get('studentId')}</div>`);
 if (url.searchParams.get('profile') === 'true') {
  if (fail) { fail = false; res.statusCode = 503; return res.end('Unavailable'); }
  const wait = delay; delay = 0;
  await new Promise(resolve => setTimeout(resolve, wait));
  if (!res.destroyed) res.end(profile(url.searchParams.get('studentId')));
  return;
 }
 res.end(`<!doctype html><html><head><meta name="viewport" content="width=device-width,initial-scale=1"><style>body{margin:0;font:16px Arial}.parent-athlete-selector{display:flex;gap:20px;padding:20px}.swipe-surface{height:240px;padding:20px;background:#eef5fa}#parent-athlete-content{touch-action:pan-y pinch-zoom}.parent-panel{width:80%}.athlete-switch-status:not(.error){position:absolute;clip-path:inset(50%)}</style></head><body><nav class="parent-athlete-selector"><a class="parent-athlete-choice selected" data-student-id="1" aria-current="page" href="/p/test?studentId=1&continueVisit=true">Child 1<small>Active</small></a><a class="parent-athlete-choice" data-student-id="2" href="/p/test?studentId=2&continueVisit=true">Child 2</a></nav>${profile(1)}<script>window.pageBoot=Math.random();</script><script src="/js/parent-events.js"></script><script src="/js/parent-panels.js"></script><script src="/js/parent-athlete-switch.js"></script></body></html>`);
});
(async () => {
 await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
 const browser = await chromium.launch({ channel: 'msedge', headless: true });
 try {
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
  const page = await context.newPage();const errors=[];page.on('pageerror', e => errors.push(e.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/p/test?studentId=1`);
  const requests=[];page.on('request',r=>{if(r.url().includes('profile=true'))requests.push(r.url());});
  const boot = await page.evaluate(() => window.pageBoot);
  const selected = async id => {try{await page.waitForFunction(value => document.getElementById('parent-athlete-content').dataset.studentId === value && !document.getElementById('parent-athlete-content').hasAttribute('aria-busy'), String(id),{timeout:5000});}catch(error){console.log({requests,errors,state:await page.evaluate(()=>({url:location.href,student:document.getElementById('parent-athlete-content')?.dataset.studentId,busy:document.getElementById('parent-athlete-content')?.getAttribute('aria-busy'),status:document.querySelector('.athlete-switch-status')?.textContent}))});throw error;}};
  await page.locator('[data-student-id="2"].parent-athlete-choice').tap();await selected(2);
  assert.equal(await page.evaluate(() => window.pageBoot), boot, 'Switching does not reload the document');
  assert.equal(await page.locator('.parent-panel').count(), 2, 'Old dialogs are removed');
  await page.locator('[aria-controls="panel-attendance"]').tap();await page.waitForSelector('#panel-attendance[open]');
  assert.match(await page.locator('#panel-attendance').innerText(), /Training 2/);
  await page.locator('#panel-attendance .panel-close').tap();await page.waitForFunction(() => !document.querySelector('dialog[open]'));
  await page.locator('[aria-controls="panel-events"]').tap();await page.waitForSelector('#panel-events .parent-event-content');
  assert.match(await page.locator('#panel-events').innerText(), /Events 2/);
  await page.locator('#panel-events .panel-close').tap();await page.waitForFunction(() => !document.querySelector('dialog[open]'));
  const cdp=await context.newCDPSession(page);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:80,y:150}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:170,y:153}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:290,y:155}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await selected(1);
  assert.equal(await page.evaluate(() => window.pageBoot), boot, 'Swiping does not reload the document');
  await page.locator('[data-student-id="2"].parent-athlete-choice').tap();await selected(2);
  delay=250;await page.locator('[data-student-id="1"].parent-athlete-choice').tap();
  await page.locator('[data-student-id="2"].parent-athlete-choice').tap();await selected(2);
  await page.waitForTimeout(300);assert.equal(await page.locator('#parent-athlete-content').getAttribute('data-student-id'),'2','Late responses cannot replace the latest choice');
  fail=true;await page.locator('[data-student-id="1"].parent-athlete-choice').tap();await page.waitForSelector('.athlete-switch-status.error');
  assert.equal(await page.locator('#parent-athlete-content').getAttribute('inert'),null,'Failed navigation restores interaction');
  await page.locator('[data-student-id="1"].parent-athlete-choice').tap();await selected(1);
  await page.goBack();await selected(2);
  assert.equal(await page.locator('.parent-panel').count(),2);
  assert.deepEqual(errors,[]);
  console.log('PASS: Mobile tap and real touch swipe switch profiles without reloading.');
  console.log('PASS: Correct child dialogs, lazy event panel, latest-response ordering, retry and Back navigation.');
 } finally { await browser.close();server.close(); }
})().catch(error => { console.error(error);server.close();process.exitCode=1; });
