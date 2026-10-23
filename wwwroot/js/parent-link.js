document.getElementById('copy-parent-link')?.addEventListener('click', async function () {
    const input = document.getElementById('parent-link');
    try { await navigator.clipboard.writeText(input.value); this.textContent = 'U kopjua'; }
    catch { input.focus(); input.select(); }
});

(() => {
    const button = document.getElementById('share-parent-qr');
    if (!button || !navigator.share || !navigator.canShare) return;
    const status = document.getElementById('parent-qr-status');
    const probe = new File([''], button.dataset.filename, { type: 'image/png' });
    if (!navigator.canShare({ files: [probe] })) return;
    button.hidden = false;
    button.addEventListener('click', async () => {
        button.disabled = true;
        status.textContent = '';
        try {
            const response = await fetch(button.dataset.url, { cache: 'no-store', credentials: 'same-origin' });
            if (!response.ok || !response.headers.get('content-type')?.startsWith('image/png')) throw new Error('QR unavailable');
            const file = new File([await response.blob()], button.dataset.filename, { type: 'image/png' });
            await navigator.share({ files: [file], title: 'Portali i prindit' });
        } catch (error) {
            if (error.name !== 'AbortError') status.textContent = 'Përdorni “Ruaj foton” dhe dërgojeni nga mesazhet.';
        } finally { button.disabled = false; }
    });
})();
