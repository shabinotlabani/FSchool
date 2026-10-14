# PWA checks

Start the application on localhost, install `@playwright/test` in a temporary tools directory, and expose that directory through `NODE_PATH`. Microsoft Edge must be installed. Run:

```powershell
node tests/Pwa.Checks/check.cjs http://localhost:5188
```

The checks cover manifest delivery and parsing, icon responses, service worker control, the install-button event flow, the public-only cache, offline navigation, failed offline POSTs, recovery after reconnecting, and the iPhone instructions at mobile width. The install prompt is simulated; actual installation must also be checked on physical devices after publishing.

## Publishing and phone verification

- Publish the application normally, including `wwwroot`. No database migration is required for PWA support.
- Use HTTPS with a valid certificate. Plain HTTP on a LAN IP does not support this installation flow; localhost is supported for development.
- Android: open the site in Chrome, sign in, and select **Instalo aplikacionin** when the browser offers installation. The browser menu also provides installation options.
- iPhone/iPad: open the site in Safari, select **Share**, then **Add to Home Screen**.
- Launch from the home-screen icon and confirm the application opens without browser navigation controls.
- After one online visit, enable airplane mode and navigate or reload. The offline notice should appear. Reconnect and select **Provo përsëri**.
- Payments, reservations, authenticated pages, and parent links are never stored by the service worker or queued for offline submission. Only the public offline page and logo are cached.
- Installation from the staff portal opens the portal root and uses existing login/session rules. Installation from a parent page uses a dedicated, non-cacheable manifest with that private link as its app ID and launch URL. No child names or financial details appear in the manifest. Treat this installed shortcut as private access to that child's report. Rotating or revoking the link disables the old shortcut; install again from the replacement link.
- For future changes to the offline page, increment the cache version in `wwwroot/sw.js`. Updates activate after old controlled windows close, avoiding forced reloads during form entry.

Reference: [MDN — Making PWAs installable](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable).
