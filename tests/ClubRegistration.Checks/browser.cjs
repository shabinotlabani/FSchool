const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const path = require('path');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch({headless:true, executablePath:process.env.MANUAL_CHROME});
  try {
    const page = await browser.newPage();
    const output = path.resolve('../.verification/registration-preview');
    fs.mkdirSync(output,{recursive:true});
    for(const width of [1280,390]) {
      await page.setViewportSize({width,height:950});
      await page.goto(process.argv[2]+'/Clubs/Register');
      await page.getByRole('heading',{name:'Regjistro Klubin',exact:true}).waitFor();
      if(await page.locator('form.registration-form input:not([type=hidden])').count() !== 4) throw Error('Expected four fields');
      if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Horizontal overflow');
      await page.screenshot({path:path.join(output,`register-${width}.png`),fullPage:true});
    }
    await page.goto(process.argv[2]+'/Clubs/RegistrationThanks');
    await page.screenshot({path:path.join(output,'thanks.png'),fullPage:true});
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
