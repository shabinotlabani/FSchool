(() => {
    const panel = document.getElementById('parent-push');
    if (!panel) return;
    const button = document.getElementById('push-toggle');
    const label = button.querySelector('span');
    const status = document.getElementById('push-status');
    const csrf = panel.querySelector('[name="__RequestVerificationToken"]').value;
    let registration, subscription, publicKey, enabled = false;
    const ios = /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    const standalone = matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
    if (ios && !standalone) { status.textContent = 'Për njoftime në iPhone: shtoni portalin në ekranin kryesor (Add to Home Screen) dhe hapeni nga ikona.'; return; }
    if (!window.isSecureContext || !('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
        status.textContent = 'Ky shfletues nuk mbështet njoftimet. Provoni nga një shfletues i përditësuar.'; return;
    }
    const post = async (url, data) => {
        const body = new URLSearchParams({ __RequestVerificationToken: csrf, ...data });
        const response = await fetch(url, { method: 'POST', body, credentials: 'same-origin' });
        if (!response.ok) {
            const error = await response.json().catch(() => null);
            throw new Error(error?.message || 'Nuk u ruajt ndryshimi. Rihapni portalin dhe provoni përsëri.');
        }
        return response.json();
    };
    const paint = () => { label.textContent = enabled ? 'Çaktivizo njoftimet' : 'Aktivizo njoftimet'; button.disabled = !publicKey; };
    const decode = value => Uint8Array.from(atob(value.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat((4-value.length%4)%4)), c=>c.charCodeAt(0));
    async function initialize() {
        try {
            await navigator.serviceWorker.register(panel.dataset.worker, { updateViaCache: 'none' });
            registration = await navigator.serviceWorker.ready;
            subscription = await registration.pushManager.getSubscription();
            const state = await post(panel.dataset.status, { Endpoint: subscription?.endpoint || '' });
            publicKey = state.publicKey; enabled = !!state.enabled && Notification.permission === 'granted';
            status.textContent = !publicKey ? 'Njoftimet do të jenë të disponueshme së shpejti.' : enabled ? 'Njoftimet janë aktive në këtë pajisje.' : '';
            paint();
        } catch (error) { status.textContent = error.message; }
    }
    button.addEventListener('click', async () => {
        button.disabled = true;
        try {
            if (enabled) {
                await post(panel.dataset.disable, { Endpoint: subscription.endpoint });
                // A shared browser subscription can belong to siblings; disable only this player.
                enabled = false; status.textContent = 'Njoftimet e këtij portali u çaktivizuan në këtë pajisje.';
            } else {
                const permission = await Notification.requestPermission();
                if (permission !== 'granted') { status.textContent = 'Lejoni njoftimet te cilësimet e shfletuesit për këtë platformë.'; return; }
                subscription = await registration.pushManager.getSubscription() || await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: decode(publicKey) });
                const json = subscription.toJSON();
                await post(panel.dataset.enable, { Endpoint: json.endpoint, P256dh: json.keys.p256dh, Auth: json.keys.auth });
                enabled = true; status.textContent = 'Njoftimet janë aktive në këtë pajisje.';
            }
        } catch (error) { status.textContent = error.message || 'Njoftimet nuk u aktivizuan. Provoni përsëri.'; }
        finally { paint(); }
    });
    initialize();
})();
