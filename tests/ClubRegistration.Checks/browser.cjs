const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const path = require('path');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch({headless:true, executablePath:process.env.MANUAL_CHROME});
  try {
    const page = await browser.newPage();
    const output = path.resolve('../.verification/registration-preview');
    fs.mkdirSync(output,{recursive:true});
    for(const width of [1536,1280,768,390]) {
      await page.setViewportSize({width,height:1024});
      await page.goto(process.argv[2]+'/Clubs/Register');
      await page.getByRole('heading',{name:'Regjistro Klubin',exact:true}).waitFor();
      await page.locator('a[href="/Clubs/Security"]').waitFor();
      if(await page.locator('form.registration-form input:not([type=hidden])').count() !== 4) throw Error('Expected four fields');
      if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Horizontal overflow');
      await page.screenshot({path:path.join(output,`register-${width}.png`),fullPage:true});
      for (const language of ['sq','en']) {
        await page.goto(process.argv[2]+'/Identity/Account/Login?language='+language);
        await page.locator('.premium-modules').waitFor();
        if(await page.locator('.premium-module').count() !== 10) throw Error('Expected ten module previews');
        for(const selector of ['.coach-scene>img','.parent-scene>img']) {
          if(!await page.locator(selector).evaluate(img=>img.complete && img.naturalWidth>0)) throw Error('Person asset did not load');
        }
        if(width <= 1100 && !await page.evaluate(()=>document.querySelector('.form-panel').getBoundingClientRect().top < document.querySelector('.premium-showcase').getBoundingClientRect().top)) throw Error('Login should be first on mobile/tablet');
        await page.locator('a[href="/Clubs/Security"]').waitFor();
        if(width === 1536) {
          const ratio=await page.evaluate(()=>{const left=document.querySelector('.premium-showcase').getBoundingClientRect().width;const right=document.querySelector('.form-panel').getBoundingClientRect().width;return left/(left+right);});
          if(ratio<0.67 || ratio>0.73) throw Error('Desktop must retain the 70/30 composition');
        }
        await page.locator('.password-toggle').click();
        if(await page.locator('#Input_Password').getAttribute('type') !== 'text') throw Error('Password visibility toggle failed');
        await page.locator('.password-toggle').click();
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
    await page.emulateMedia({reducedMotion:'reduce'});
    await page.goto(process.argv[2]+'/Identity/Account/Login');
    if(await page.locator('.premium-module').first().evaluate(el=>getComputedStyle(el).animationName) !== 'none') throw Error('Reduced motion preference ignored');
    await page.goto(process.argv[2]+'/Clubs/RegistrationThanks');
    await page.screenshot({path:path.join(output,'thanks.png'),fullPage:true});
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
