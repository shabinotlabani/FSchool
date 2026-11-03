(() => {
    const panel = document.getElementById('duplicate-review');
    if (!panel?.dataset.checkUrl) return;
    const form = panel.closest('form');
    const fields = ['FirstName', 'LastName', 'Phone', 'ParentPhone'].map(name => form.elements.namedItem(name));
    let timer, controller, version = 0;
    async function check(current) {
        controller = new AbortController();
        const data = new FormData();
        fields.forEach(field => data.append(field.name, field.value));
        data.append('__RequestVerificationToken', form.elements.namedItem('__RequestVerificationToken').value);
        try {
            const response = await fetch(panel.dataset.checkUrl, { method: 'POST', body: data, signal: controller.signal, credentials: 'same-origin' });
            if (!response.ok || response.redirected) throw new Error('Duplicate check failed');
            const html = await response.text();
            if (current === version) panel.innerHTML = html;
        } catch (error) {
            if (error.name !== 'AbortError' && current === version) {
                panel.textContent = 'Kontrolli i përputhjeve do të bëhet kur të shtypni “Regjistro lojtarin”.';
            }
        }
    }
    fields.forEach(field => field.addEventListener('input', () => {
        clearTimeout(timer);
        controller?.abort();
        const current = ++version;
        // Never retain acknowledgement after changing the identity or phone.
        const confirmed = panel.querySelector('[name=ConfirmDuplicate]');
        if (confirmed) confirmed.checked = false;
        timer = setTimeout(() => check(current), 400);
    }));
    // Preserve the server warning after a rejected submission; check prefilled forms otherwise.
    if (!panel.querySelector('section') && fields.some(field => field.value.trim())) check(version);
})();
