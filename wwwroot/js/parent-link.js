document.getElementById('copy-parent-link')?.addEventListener('click', async function () {
    const input = document.getElementById('parent-link');
    try { await navigator.clipboard.writeText(input.value); this.textContent = 'U kopjua'; }
    catch { input.focus(); input.select(); }
});
