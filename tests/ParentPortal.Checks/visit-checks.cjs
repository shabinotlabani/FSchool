const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../wwwroot/js/parent-portal-visit.js'), 'utf8');
function page({ url = '/p/private', navigation = 'navigate' } = {}) {
    const events = {}, redirects = [];
    const document = { body: { dataset: { parentReopenUrl: url } }, visibilityState: 'visible', addEventListener: (name, fn) => events[name] = fn };
    const window = { location: { replace: url => redirects.push(url) }, addEventListener: (name, fn) => events[name] = fn };
    vm.runInNewContext(source, { document, window, performance: { getEntriesByType: () => [{ type: navigation }] } });
    return { document, events, redirects };
}
let p = page();
assert.equal(p.redirects.length, 0, 'Internal navigation must not reopen the notice');
p.document.visibilityState = 'hidden'; p.events.visibilitychange();
assert.equal(p.redirects.length, 0);
p.document.visibilityState = 'visible'; p.events.visibilitychange();
assert.deepEqual(p.redirects, ['/p/private'], 'Resuming the platform requests a fresh notice');
p.events.pageshow({ persisted: true });
assert.equal(p.redirects.length, 1, 'Repeated lifecycle events only redirect once');
p = page(); p.events.pageshow({ persisted: true });
assert.deepEqual(p.redirects, ['/p/private'], 'Restoring a cached page requests a fresh notice');
assert.deepEqual(page({ navigation: 'reload' }).redirects, ['/p/private']);
assert.equal(page({ url: '', navigation: 'reload' }).redirects.length, 0, 'The notice itself never enters a reload loop');
console.log('PASS: navigation, app resume, cached restoration, reload, and notice-loop prevention');
