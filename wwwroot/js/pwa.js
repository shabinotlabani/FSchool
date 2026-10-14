(() => {
    const script = document.currentScript;
    const panel = document.querySelector('.pwa-install');
    const button = panel.querySelector('.pwa-install-button');
    const iosHelp = panel.querySelector('.pwa-ios-help');
    const standalone = () => window.matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
    let promptEvent;
    let dismissed = false;
    try { dismissed = sessionStorage.getItem('pwa-install-dismissed') === '1'; } catch { /* Storage may be unavailable. */ }
    const hide = () => { panel.hidden = true; };
    window.addEventListener('beforeinstallprompt', event => {
        event.preventDefault();
        promptEvent = event;
        if (!standalone() && !dismissed) {
            button.hidden = false;
            iosHelp.hidden = true;
            panel.hidden = false;
        }
    });
    button.addEventListener('click', async () => {
        if (!promptEvent) return;
        button.disabled = true;
        try {
            await promptEvent.prompt();
            await promptEvent.userChoice;
        } finally {
            promptEvent = null;
            button.disabled = false;
            hide();
        }
    });
    panel.querySelector('.pwa-dismiss').addEventListener('click', () => {
        dismissed = true;
        hide();
        try { sessionStorage.setItem('pwa-install-dismissed', '1'); } catch { /* Optional preference. */ }
    });
    window.addEventListener('appinstalled', () => { promptEvent = null; hide(); });
    window.matchMedia('(display-mode: standalone)').addEventListener('change', () => { if (standalone()) hide(); });
    const ios = /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    if (window.isSecureContext && ios && !standalone() && !dismissed) {
        iosHelp.hidden = false;
        panel.hidden = false;
    }
    if ('serviceWorker' in navigator && window.isSecureContext) {
        navigator.serviceWorker.register(script.dataset.serviceWorker, { updateViaCache: 'none' })
            .catch(error => console.warn('PWA registration failed:', error));
    }
})();
