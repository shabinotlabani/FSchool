import { chromium } from 'file:///C:/Users/OnLine/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const b=await chromium.launch({channel:'msedge',headless:true});
const page=await b.newPage({viewport:{width:1360,height:820},deviceScaleFactor:1.5});
for(const kind of ['coach','report']){
 await page.goto('http://127.0.0.1:5199/preview/'+kind,{waitUntil:'networkidle'});
 await page.addStyleTag({url:'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css'});
 for(const css of ['site','brand','platform'])await page.addStyleTag({url:`http://127.0.0.1:5199/css/${css}.css`});
 console.log(kind,await page.title(),(await page.locator('body').innerText()).slice(0,120));
 await page.locator(kind==='coach'?'.coach-role-page':'.coach-activity-report').screenshot({path:`presentations/Premium_Sales/Presentation_Assets/${kind}-screenshot.png`});
}
await b.close();
