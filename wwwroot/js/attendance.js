(() => {
    const form = document.getElementById('attendance-form');
    if (!form) return;
    const histories = [...form.querySelectorAll('.player-history')];
    histories.forEach(history => history.addEventListener('toggle', () => {
        if (history.open) histories.filter(other => other !== history).forEach(other => { other.open = false; });
    }));
    document.addEventListener('click', event => {
        histories.filter(history => !history.contains(event.target)).forEach(history => { history.open = false; });
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') histories.filter(history => history.open).forEach(history => {
            history.open = false;
            history.querySelector('summary').focus();
        });
    });
    const rows = [...form.querySelectorAll('[data-player]')];
    const count = document.getElementById('attendance-count');
    const next = document.getElementById('next-unmarked');
    let dirty = false;
    const missing = () => rows.filter(row => !row.querySelector('input[type=radio]:checked'));
    function update() {
        if (!count) return;
        const present = form.querySelectorAll('input[value=true]:checked').length;
        const absent = form.querySelectorAll('input[value=false]:checked').length;
        count.textContent = `${present} prezent · ${absent} mungon · ${rows.length - present - absent} pa shënuar`;
        next.hidden = missing().length === 0;
    }
    form.addEventListener('change', () => { dirty = true; document.getElementById('attendance-dirty').hidden = false; update(); });
    next?.addEventListener('click', () => {
        const row = missing()[0];
        if (row) { row.scrollIntoView({behavior:'smooth', block:'center'}); row.querySelector('input[type=radio]').focus({preventScroll:true}); }
    });
    form.addEventListener('submit', () => { dirty = false; });
    window.addEventListener('beforeunload', event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } });
    update();
    const deadline = Date.parse(form.dataset.editableUntil);
    function lockExpired() {
        if (!Number.isFinite(deadline) || Date.now() < deadline) return;
        form.querySelectorAll('input[type=radio]').forEach(input => { input.disabled = true; });
        const actions = form.querySelector('.attendance-save');
        if (actions) actions.hidden = true;
        document.getElementById('attendance-lock').hidden = false;
    }
    lockExpired();
    if (Number.isFinite(deadline)) setInterval(lockExpired, 1000);
})();
