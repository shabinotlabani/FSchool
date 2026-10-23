/* Only public fallback assets are cached. Account pages and mutations always use the network. */
const CACHE = 'upnext-offline-v5';
const offlineUrl = new URL('offline.html', self.registration.scope).href;
const logoUrl = new URL('images/upnext/app_icon_192.png', self.registration.scope).href;
self.addEventListener('install', event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll([offlineUrl, logoUrl])).then(()=>self.skipWaiting()));
});

self.addEventListener('push', event => {
    let data = {};
    try { data = event.data?.json() || {}; } catch { /* Always show a visible notification. */ }
    const target = new URL(data.url || './', self.registration.scope);
    const safe = target.origin === self.location.origin && /^\/p\/[a-fA-F0-9]{64}$/.test(target.pathname);
    const icon = new URL(data.icon || logoUrl, self.registration.scope);
    const safeIcon = icon.origin === self.location.origin && icon.pathname.endsWith('/ClubSettings/Logo');
    event.waitUntil(self.registration.showNotification(data.title || 'Njoftim nga klubi', {
        body: data.body || 'Keni një njoftim të ri nga klubi.',
        icon: safeIcon ? icon.href : logoUrl,
        tag: data.tag || '2korriku-notification',
        data: { url: safe ? target.href : self.registration.scope }
    }));
});
self.addEventListener('notificationclick', event => {
    event.notification.close();
    const target = new URL(event.notification.data?.url || './', self.registration.scope);
    if (target.origin !== self.location.origin) return;
    event.waitUntil(self.clients.openWindow(target.href));
});
self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        for (const key of await caches.keys()) {
            if ((key.startsWith('2korriku-offline-') || key.startsWith('academix-offline-') || key.startsWith('upnext-offline-')) && key !== CACHE) await caches.delete(key);
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
    } else if (request.url === logoUrl) {
        event.respondWith(fetch(request, { cache: 'no-cache' }).then(async response => {
            if (response.ok) { const cache = await caches.open(CACHE); await cache.put(request, response.clone()); }
            return response;
        }).catch(async () => (await caches.match(request)) || Response.error()));
    } else if (request.url === offlineUrl) {
        event.respondWith(caches.match(request).then(cached => cached || fetch(request)));
    }
});
