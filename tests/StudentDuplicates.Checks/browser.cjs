const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const fs=require('fs');const path=require('path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.MANUAL_CHROME});
 try{
  const page=await browser.newPage();
  await page.goto(process.argv[2]+'/Identity/Account/Login');
  await page.locator('#Input_Email').fill('admin@manual.test');
  await page.locator('#Input_Password').fill('ManualTest123!');
  await Promise.all([page.waitForURL(url=>!url.pathname.endsWith('/Login')),page.locator('button[type=submit]').click()]);
  const output=path.resolve('../.verification/student-duplicates-preview');fs.mkdirSync(output,{recursive:true});
  for(const width of [1280,390]){
   await page.setViewportSize({width,height:950});
   await page.goto(process.argv[2]+'/Students/Create');
   await page.locator('#FirstName').fill('Different');await page.locator('#LastName').fill('Person');
   await page.locator('#Phone').fill('044123456');
   await page.locator('#duplicate-review table').waitFor();
   const confirm=page.locator('[name=ConfirmDuplicate]');await confirm.check();
   await page.locator('#FirstName').fill('Changed again');
   if(await confirm.isChecked())throw Error('Acknowledgement was not reset');
   await page.waitForTimeout(700);
   await page.locator('#duplicate-review').scrollIntoViewIfNeeded();
   if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Page overflow');
   await page.screenshot({path:path.join(output,`duplicates-${width}.png`)});
  }
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
