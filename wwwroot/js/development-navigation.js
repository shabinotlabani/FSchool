(() => {
    const back = document.getElementById('back-to-attendance');
    if (!back) return;
    try {
        const explicit = new URLSearchParams(location.search).get('returnUrl');
        const saved = explicit || sessionStorage.getItem('development-return-' + back.dataset.student);
        if (!saved) return;
        const url = new URL(saved, location.origin);
        if (url.origin === location.origin && url.pathname.toLowerCase() === '/coaches/players') {
            back.href = url.href;
            back.textContent = '← Lojtarët e mi';
            sessionStorage.setItem('development-return-' + back.dataset.student, url.pathname + url.search);
        }
        if (url.origin === location.origin && url.pathname.toLowerCase() === '/attendance') back.href = url.href;
        if (url.origin === location.origin && url.pathname.toLowerCase() === '/attendance/index') back.href = url.href;
    } catch { /* The server-rendered link remains available. */ }
})();
(() => {
    const role = document.getElementById('player-role');
    const primary = document.getElementById('primary-position');
    const secondary = document.getElementById('secondary-position');
    if (!role || !primary || !secondary) return;
    function update() {
        const outfield = role.value !== '' && role.value !== 'Portier';
        document.querySelectorAll('[data-outfield-position]').forEach(row => { row.hidden = !outfield; });
        primary.disabled = secondary.disabled = !outfield;
        primary.required = outfield;
        [...secondary.options].forEach(option => { option.disabled = option.value === primary.value; });
        if (secondary.value === primary.value) secondary.value = 'Asnjë';
    }
    role.addEventListener('change', update);
    primary.addEventListener('change', update);
    update();
})();
