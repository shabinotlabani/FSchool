/* Only public fallback assets are cached. Account pages and mutations always use the network. */
const CACHE = '2korriku-offline-v1';
const offlineUrl = new URL('offline.html', self.registration.scope).href;
const logoUrl = new URL('images/fc-2-korriku.svg', self.registration.scope).href;
self.addEventListener('install', event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll([offlineUrl, logoUrl])));
});
self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        for (const key of await caches.keys()) {
            if (key.startsWith('2korriku-offline-') && key !== CACHE) await caches.delete(key);
        }
        await self.clients.claim();
    })());
});
self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || new URL(request.url).origin !== self.location.origin) return;
    if (request.mode === 'navigate') {
        event.respondWith(fetch(request, { cache: 'no-store' }).catch(async () =>
            (await caches.match(offlineUrl)) || Response.error()));
    } else if (request.url === logoUrl || request.url === offlineUrl) {
        event.respondWith(caches.match(request).then(cached => cached || fetch(request)));
    }
});
