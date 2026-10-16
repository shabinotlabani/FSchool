(() => {
    const entryUrl = document.body.dataset.parentReopenUrl;
    if (!entryUrl) return;
    let reopening = false;
    const reopen = () => {
        if (reopening) return;
        reopening = true;
        window.location.replace(entryUrl);
    };
    // Mobile apps may resume the existing page instead of requesting their start URL.
    let wasHidden = document.visibilityState === 'hidden';
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') wasHidden = true;
        else if (wasHidden) reopen();
    });
    window.addEventListener('pageshow', event => {
        if (event.persisted) reopen();
    });
    // Refreshing an acknowledged page is also a fresh opening.
    if (performance.getEntriesByType('navigation')[0]?.type === 'reload') reopen();
})();
