const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const fs=require('fs');const path=require('path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.MANUAL_CHROME});
 try{
  const page=await browser.newPage();
  await page.goto(process.argv[2]+'/Identity/Account/Login');
  await page.locator('#Input_Email').fill('admin@manual.test');
  await page.locator('#Input_Password').fill('ManualTest123!');
  await Promise.all([page.waitForURL(url=>!url.pathname.endsWith('/Login')),page.locator('#login-form button[type=submit]').click()]);
  const output=path.resolve('../.verification/dashboard-trend-preview');fs.mkdirSync(output,{recursive:true});
  for(const width of [1440,390]){
   await page.setViewportSize({width,height:1000});await page.goto(process.argv[2]+'/');
   await page.locator('.registration-trend').waitFor();
   if(await page.locator('.trend-column').count()!==6)throw Error('Expected six months');
   if(await page.locator('.trend-summary strong').first().innerText()!=='1')throw Error('Current registrations incorrect');
   const bounds=await page.locator('.registration-trend').boundingBox();
   if(bounds.x<0||bounds.x+bounds.width>width+1)throw Error('Chart overflows viewport');
   await page.locator('.registration-trend').screenshot({path:path.join(output,`chart-${width}.png`)});
  }
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
