(() => {
    const weeks = document.getElementById('Weeks');
    const date = document.getElementById('Date');
    const price = document.getElementById('Price');
    const start = document.getElementById('StartsAt');
    const end = document.getElementById('EndsAt');
    const preview = document.getElementById('booking-repeat-preview');
    if (!weeks || !preview) return;
    function update() {
        const count = Number(weeks.value);
        if (!date.value || !Number.isInteger(count) || count < 1 || count > 52) { preview.textContent = ''; return; }
        const first = new Date(date.value + 'T12:00:00');
        if (Number.isNaN(first.getTime())) { preview.textContent = ''; return; }
        const days = ['E diel', 'E hënë', 'E martë', 'E mërkurë', 'E enjte', 'E premte', 'E shtunë'];
        const format = d => `${String(d.getDate()).padStart(2,'0')}.${String(d.getMonth()+1).padStart(2,'0')}.${d.getFullYear()}`;
        const last = new Date(first); last.setDate(last.getDate() + (count - 1) * 7);
        const amount = Number(price.value);
        const cost = Number.isFinite(amount) && amount >= 0 ? ` · Gjithsej: ${(amount * count).toLocaleString('sq-AL', {minimumFractionDigits:2, maximumFractionDigits:2})} € (${count} × ${amount.toLocaleString('sq-AL')} €)` : '';
        preview.textContent = `${count} termine · ${days[first.getDay()]} · ${format(first)}${count > 1 ? ' – ' + format(last) : ''}${start.value && end.value ? ' · ' + start.value + '–' + end.value : ''}${cost}. Çdo termin paguhet ose anulohet veçmas.`;
    }
    [weeks, date, price, start, end].forEach(input => input.addEventListener('input', update));
    update();
})();
