(() => {
    const form = document.getElementById('field-booking-form');
    if (!form) return;
    const weeks = document.getElementById('Weeks');
    const date = document.getElementById('Date');
    const price = document.getElementById('Price');
    const start = document.getElementById('StartsAt');
    const field = document.getElementById('FootballFieldId');
    const preview = document.getElementById('booking-repeat-preview');
    const timePreview = document.getElementById('booking-time-preview');
    const repeat = document.getElementById('booking-repeat');
    const days = [...document.querySelectorAll('input[name="Days"]')];
    const timeButtons = [...document.querySelectorAll('[data-booking-time]')];
    const chipRows = [...document.querySelectorAll('.field-time-chips')];
    const format = value => value.toLocaleDateString('sq-AL');
    let occupied = new Set();
    let availabilityRequest = 0;

    if (form.dataset.new === 'true' && field && !field.value) {
        const choices = [...field.options].filter(option => option.value && !option.textContent.includes('(Joaktive)'));
        if (choices.length === 1) field.value = choices[0].value;
    }

    function timeRange() {
        if (!start?.value) return '';
        const [hour, minute] = start.value.split(':').map(Number);
        return `${start.value}–${String((hour + 1) % 24).padStart(2, '0')}:${String(minute).padStart(2, '0')}${hour === 23 ? ' · dita tjetër' : ''}`;
    }

    function paintTimes() {
        timeButtons.forEach(button => {
            const isOccupied = occupied.has(button.dataset.bookingTime);
            button.disabled = isOccupied;
            button.classList.toggle('is-occupied', isOccupied);
            button.classList.toggle('is-selected', !isOccupied && button.dataset.bookingTime === start?.value);
            button.setAttribute('aria-label', `${button.dataset.bookingTime}${isOccupied ? ', e zënë' : ', e lirë'}`);
            button.title = isOccupied ? 'Kjo orë është e zënë' : 'Orë e lirë';
        });
    }

    async function loadAvailability() {
        occupied = new Set();
        paintTimes();
        if (!field?.value || !date?.value || !form.dataset.availabilityUrl) return;
        const request = ++availabilityRequest;
        chipRows.forEach(row => row.classList.add('is-loading'));
        try {
            const query = new URLSearchParams({ fieldId: field.value, date: date.value, bookingId: document.getElementById('Id')?.value || '0' });
            const response = await fetch(`${form.dataset.availabilityUrl}?${query}`, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('availability');
            const result = await response.json();
            if (request !== availabilityRequest) return;
            occupied = new Set(result.occupied || []);
            if (start?.value && occupied.has(start.value)) {
                start.value = '';
                start.setCustomValidity('Zgjidhni një orë të lirë.');
            } else start?.setCustomValidity('');
            paintTimes();
            update();
        } catch {
            if (request === availabilityRequest && timePreview) timePreview.textContent = 'Orari nuk mundi të kontrollohej.';
        } finally {
            if (request === availabilityRequest) chipRows.forEach(row => row.classList.remove('is-loading'));
        }
    }

    function update() {
        const range = timeRange();
        if (timePreview) timePreview.textContent = range ? `${range} · 60 minuta` : '';
        if (start?.value && occupied.has(start.value)) start.setCustomValidity('Kjo orë është e zënë.');
        else start?.setCustomValidity('');
        paintTimes();
        if (!preview) return;
        if (!weeks) { preview.textContent = range; return; }
        const count = Number(weeks.value);
        const selected = days.filter(day => day.checked).map(day => Number(day.value));
        if (!date.value || !Number.isInteger(count) || count < 1 || count > 52 || !selected.length) {
            preview.textContent = 'Plotëso datën, orën dhe përsëritjen.';
            return;
        }
        const first = new Date(`${date.value}T12:00:00`);
        const dates = [];
        if (Number.isNaN(first.getTime())) return;
        for (let offset = 0; offset < count * 7; offset++) {
            const day = new Date(first);
            day.setDate(day.getDate() + offset);
            if (selected.includes((day.getDay() + 6) % 7 + 1)) dates.push(day);
        }
        if (!dates.length) { preview.textContent = 'Nuk u gjet asnjë datë për ditët e zgjedhura.'; return; }
        const amount = Number(price.value);
        const cost = Number.isFinite(amount) && amount >= 0 ? ` · ${new Intl.NumberFormat('sq-AL', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount * dates.length)}` : '';
        preview.textContent = dates.length === 1
            ? `${format(dates[0])}${range ? ` · ${range}` : ''}${cost}`
            : `${dates.length} termine · ${format(dates[0])}–${format(dates.at(-1))}${range ? ` · ${range}` : ''}${cost}`;
        if (repeat && (count > 1 || selected.length > 1)) repeat.open = true;
    }

    timeButtons.forEach(button => button.addEventListener('click', () => {
        if (button.disabled) return;
        start.value = button.dataset.bookingTime;
        start.dispatchEvent(new Event('input', { bubbles: true }));
    }));
    [weeks, price, start, ...days].filter(Boolean).forEach(input => input.addEventListener('input', update));
    [field, date].filter(Boolean).forEach(input => input.addEventListener('change', loadAvailability));
    form.addEventListener('submit', event => {
        if (start?.value && occupied.has(start.value)) {
            event.preventDefault();
            start.setCustomValidity('Kjo orë është e zënë.');
            start.reportValidity();
        }
    });
    update();
    loadAvailability();
})();
