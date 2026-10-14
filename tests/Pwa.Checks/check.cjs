// Run with @playwright/test available: node tests/Pwa.Checks/check.cjs http://localhost:5188
const { chromium } = require('@playwright/test');
const assert = require('node:assert/strict');
const base = process.argv[2] || 'http://localhost:5188';
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(`${base}/Identity/Account/Login`);
        await page.evaluate(async () => { await navigator.serviceWorker.ready; });
        await page.reload();
        await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
        const manifestResponse = await context.request.get(`${base}/manifest.webmanifest`);
        assert.match(manifestResponse.headers()['content-type'], /application\/manifest\+json/);
        const manifest = await manifestResponse.json();
        assert.equal(manifest.display, 'standalone');
        for (const icon of manifest.icons) {
            const response = await context.request.get(`${base}/${icon.src}`);
            assert.equal(response.status(), 200);
            assert.match(response.headers()['content-type'], /image\/png/);
        }
        const client = await context.newCDPSession(page);
        const appManifest = await client.send('Page.getAppManifest');
        assert.deepEqual(appManifest.errors, []);
        console.log('PASS: Manifest, icons, and service worker load on the login page');
        await page.evaluate(() => {
            const event = new Event('beforeinstallprompt', { cancelable: true });
            event.prompt = async () => { window.installPromptCalled = true; };
            event.userChoice = Promise.resolve({ outcome: 'accepted' });
            window.dispatchEvent(event);
        });
        await page.getByRole('button', { name: 'Instalo aplikacionin', exact: true }).click();
        assert.equal(await page.evaluate(() => window.installPromptCalled), true);
        assert.equal(await page.locator('.pwa-install').isVisible(), false);
        console.log('PASS: Install button invokes the browser prompt and hides afterward');
        const cachedUrls = await page.evaluate(async () => {
            const urls = [];
            for (const key of await caches.keys()) {
                for (const request of await (await caches.open(key)).keys()) urls.push(new URL(request.url).pathname);
            }
            return urls.sort();
        });
        assert.deepEqual(cachedUrls, ['/images/fc-2-korriku.svg', '/offline.html']);
        await context.setOffline(true);
        await page.goto(`${base}/Fields`);
        assert.equal(await page.getByRole('heading', { name: 'Nuk ka lidhje me internetin' }).count(), 1);
        const mutationFailed = await page.evaluate(async () => {
            try { await fetch('/Fields/Booking', { method: 'POST', body: '' }); return false; }
            catch { return true; }
        });
        assert.equal(mutationFailed, true);
        await context.setOffline(false);
        await page.getByRole('button', { name: 'Provo përsëri' }).click();
        await page.waitForURL('**/Identity/Account/Login?**');
        console.log('PASS: Offline navigation shows fallback, mutations fail, and reconnect restores login');
        assert.deepEqual(errors, []);
        const ios = await browser.newContext({ viewport: { width: 390, height: 844 }, userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1' });
        const iosPage = await ios.newPage();
        await iosPage.goto(`${base}/Identity/Account/Login`);
        assert.equal(await iosPage.locator('.pwa-ios-help').isVisible(), true);
        assert.equal(await iosPage.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        await iosPage.getByRole('button', { name: 'Mbyll njoftimin e instalimit' }).click();
        await iosPage.reload();
        assert.equal(await iosPage.locator('.pwa-install').isVisible(), false);
        console.log('PASS: iOS instructions fit mobile width and dismissal persists for the session');
        await context.close();
        await ios.close();
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
