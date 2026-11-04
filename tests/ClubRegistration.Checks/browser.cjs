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
      await page.locator('a[href="/Clubs/Security"]').waitFor();
      if(await page.locator('form.registration-form input:not([type=hidden])').count() !== 4) throw Error('Expected four fields');
      if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Horizontal overflow');
      await page.screenshot({path:path.join(output,`register-${width}.png`),fullPage:true});
      for (const language of ['sq','en']) {
        await page.goto(process.argv[2]+'/Identity/Account/Login?language='+language);
        await page.locator('.product-reasons').waitFor();
        await page.locator('a[href="/Clubs/Security"]').waitFor();
        if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Login horizontal overflow');
        await page.screenshot({path:path.join(output,`login-${language}-${width}.png`),fullPage:true,animations:'disabled'});
      }
      for (const section of ['Security','Team']) {
        await page.goto(process.argv[2]+'/Clubs/'+section);
        const official = section === 'Security' ? 'https://trust.railway.com' : 'https://www.volcanopixels.com/team';
        await page.locator(`a[href="${official}"]`).waitFor();
        if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Information page horizontal overflow');
        await page.screenshot({path:path.join(output,`${section}-${width}.png`),fullPage:true,animations:'disabled'});
      }
    }
    await page.goto(process.argv[2]+'/Clubs/RegistrationThanks');
    await page.screenshot({path:path.join(output,'thanks.png'),fullPage:true});
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
