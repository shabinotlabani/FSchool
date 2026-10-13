(() => {
    const weeks = document.getElementById('Weeks'), date = document.getElementById('Date');
    const price = document.getElementById('Price'), start = document.getElementById('StartsAt');
    const preview = document.getElementById('booking-repeat-preview'), time = document.getElementById('booking-time-preview');
    const days = [...document.querySelectorAll('input[name="Days"]')];
    if (!preview || !start) return;
    const format = d => d.toLocaleDateString('sq-AL');
    function update() {
        let range = '';
        if (start.value) {
            const [h,m] = start.value.split(':').map(Number);
            if (h < 23) range = start.value + '–' + String(h+1).padStart(2,'0') + ':' + String(m).padStart(2,'0');
        }
        time.textContent = range ? range + ' · 60 minuta' : '';
        if (!weeks) { preview.textContent = range; return; }
        const count = Number(weeks.value), selected = days.filter(d=>d.checked).map(d=>Number(d.value));
        if (!date.value || !Number.isInteger(count) || count < 1 || count > 52 || !selected.length) {
            preview.textContent = 'Zgjidhni ditët, datën dhe numrin e javëve.'; return;
        }
        const first = new Date(date.value + 'T12:00:00'), dates = [];
        if (Number.isNaN(first.getTime())) return;
        for (let i=0;i<count*7;i++) {
            const day = new Date(first); day.setDate(day.getDate()+i);
            if(selected.includes((day.getDay()+6)%7+1)) dates.push(day);
        }
        const names = days.filter(d=>d.checked).map(d=>document.querySelector('label[for="'+d.id+'"]').textContent);
        const amount = Number(price.value);
        const cost = Number.isFinite(amount) && amount >= 0 ? ' · Gjithsej ' + (amount*dates.length).toLocaleString('sq-AL',{minimumFractionDigits:2,maximumFractionDigits:2}) : '';
        preview.textContent = names.join(' + ') + (range ? ' · '+range : '') + ' · ' + count + ' javë = ' + dates.length + ' termine · ' + format(dates[0]) + ' – ' + format(dates.at(-1)) + cost;
    }
    [weeks,date,price,start,...days].filter(Boolean).forEach(input=>input.addEventListener('input',update));
    update();
})();