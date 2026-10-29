(() => {
    const form = document.getElementById('attendance-form');
    if (!form) return;
    const menus = [...form.querySelectorAll('.attendance-more')];
    menus.forEach(menu => menu.addEventListener('toggle', () => {
        if (menu.open) menus.filter(other => other !== menu).forEach(other => { other.open = false; });
    }));
    document.addEventListener('click', event => menus.filter(menu => !menu.contains(event.target)).forEach(menu => { menu.open = false; }));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') menus.filter(menu => menu.open).forEach(menu => { menu.open = false; menu.querySelector('summary').focus(); });
    });
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
    const markAll = document.getElementById('mark-all-present');
    let dirty = false;
    const draftKey = 'attendance-draft-' + location.pathname + location.search;
    const rosterToken = form.querySelector('[name=RosterToken]')?.value;
    const revision = form.querySelector('[name=Revision]')?.value;
    const attendanceDate = form.querySelector('[name=Date]')?.value;
    try {
        const draft = JSON.parse(sessionStorage.getItem(draftKey) || 'null');
        if (draft && draft.rosterToken === rosterToken && draft.revision === revision && draft.attendanceDate === attendanceDate) {
            draft.selected.forEach(saved => {
                const input = document.getElementById(saved.id);
                if (input && input.type === 'radio' && !input.disabled && input.value === saved.value) input.checked = true;
            });
            dirty = draft.selected.length > 0;
            const indicator = document.getElementById('attendance-dirty');
            if (indicator) indicator.hidden = !dirty;
        }
        sessionStorage.removeItem(draftKey);
    } catch { /* Storage may be unavailable. */ }
    form.querySelectorAll('[data-development-student]').forEach(link => link.addEventListener('click', () => {
        try {
            sessionStorage.setItem('development-return-' + link.dataset.developmentStudent, location.pathname + location.search);
            if (dirty) sessionStorage.setItem(draftKey, JSON.stringify({ rosterToken, revision, attendanceDate,
                selected: [...form.querySelectorAll('input[type=radio]:checked')].map(input => ({ id: input.id, value: input.value })) }));
            dirty = false;
        } catch { /* Keep the unsaved-changes warning when storage fails. */ }
    }));
    const missing = () => rows.filter(row => !row.querySelector('input[type=radio]:checked'));
    function update() {
        if (!count) return;
        const present = form.querySelectorAll('input[value=true]:checked').length;
        const absent = form.querySelectorAll('input[value=false]:checked').length;
        count.textContent = `${present} prezent · ${absent} mungon · ${rows.length - present - absent} pa shënuar`;
        next.hidden = missing().length === 0;
    }
    form.addEventListener('change', () => { dirty = true; document.getElementById('attendance-dirty').hidden = false; update(); });
    markAll?.addEventListener('click', () => {
        rows.forEach(row => { const input = row.querySelector('input[value=true]:not(:disabled)'); if (input) input.checked = true; });
        dirty = true;
        document.getElementById('attendance-dirty').hidden = false;
        update();
        missing()[0]?.scrollIntoView({behavior:'smooth', block:'center'});
    });
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
