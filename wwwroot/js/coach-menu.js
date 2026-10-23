(() => {
    const status = document.getElementById('coach-menu-status');
    if (!status) return;
    let attempts = 0;
    const poll = async () => {
        if (++attempts > 60) return;
        try {
            const response = await fetch(status.dataset.statusUrl, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (!response.ok) return;
            const data = await response.json();
            if (!['Pending', 'Processing'].includes(data.status)) { window.location.reload(); return; }
            status.textContent = data.status === 'Processing' ? 'AI po përgatit menunë…' : 'Propozimi është në pritje…';
        } catch { /* Keep the manual refresh available. */ }
        window.setTimeout(poll, 5000);
    };
    window.setTimeout(poll, 5000);
})();
