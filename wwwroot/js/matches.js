(() => {
    const form = document.getElementById('match-form');
    if (!form) return;
    const rows = [...form.querySelectorAll('[data-match-player]')];
    let dirty = false;
    function reindex(row) {
        row.querySelectorAll('.match-stint').forEach((stint, i) => {
            stint.querySelectorAll('input[type=hidden][name=__Invariant]').forEach(input => input.remove());
            stint.querySelectorAll('input[type=number]').forEach((input, n) => { input.name = `Players[${row.dataset.index}].Intervals[${i}].${n === 0 ? 'From' : 'To'}`; });
        });
    }
    function add(row, starter = false) {
        const holder = document.createElement('div');
        holder.className = 'match-stint';
        holder.innerHTML = '<input type="number" min="0" max="300" aria-label="Minuta e hyrjes" placeholder="Hyrja"><span>→</span><input type="number" min="0" max="300" aria-label="Minuta e daljes" placeholder="Dalja"><button type="button" data-remove-stint aria-label="Hiq intervalin">×</button>';
        if (starter) {
            holder.querySelectorAll('input[type=number]')[0].value = '0';
            holder.querySelectorAll('input[type=number]')[1].value = form.querySelector('[name=Duration]').value;
        }
        row.querySelector('.match-stints').append(holder);
        reindex(row);
    }
    function update() {
        let starters = 0, substitutes = 0;
        rows.forEach(row => {
            const role = row.querySelector('.match-role').value;
            const playing = role === 'Starter' || role === 'Substitute';
            starters += role === 'Starter' ? 1 : 0;
            substitutes += role === 'Substitute' ? 1 : 0;
            row.querySelector('[data-add-stint]').hidden = !playing;
            let minutes = 0;
            row.querySelectorAll('.match-stint').forEach(stint => {
                const [from, to] = stint.querySelectorAll('input[type=number]');
                if (from.value !== '' && to.value !== '') minutes += Math.max(0, Number(to.value) - Number(from.value));
            });
            row.querySelector('output').textContent = minutes;
        });
        document.getElementById('match-lineup-count').textContent = `${starters} titullarë · ${substitutes} nga stoli`;
        form.dispatchEvent(new CustomEvent('match:updated'));
    }
    form.addEventListener('change', event => {
        dirty = true;
        const row = event.target.closest('[data-match-player]');
        if (row && event.target.matches('.match-role')) {
            const role = event.target.value;
            if (role !== 'Starter') row.querySelector('.pitch-slot').value = '';
            if (role === 'NotSelected' || role === 'Unused') row.querySelector('.match-stints').replaceChildren();
            else if (!row.querySelector('.match-stint')) add(row, role === 'Starter');
            else if (role === 'Starter') row.querySelector('.match-stint input[type=number]').value = '0';
        }
        update();
    });
    form.addEventListener('input', () => { dirty = true; update(); });
    form.addEventListener('click', event => {
        const row = event.target.closest('[data-match-player]');
        if (!row) return;
        if (event.target.closest('[data-add-stint]')) { add(row); dirty = true; row.querySelector('.match-stint:last-child input[type=number]').focus(); }
        if (event.target.closest('[data-remove-stint]')) { event.target.closest('.match-stint').remove(); reindex(row); dirty = true; }
        update();
    });
    form.addEventListener('submit', () => { dirty = false; });
    document.getElementById('apply-substitution').addEventListener('click', () => {
        const out = document.getElementById('sub-out').value;
        const into = document.getElementById('sub-in').value;
        const minute = Number(document.getElementById('sub-minute').value);
        const duration = Number(form.querySelector('[name=Duration]').value);
        const message = document.getElementById('sub-message');
        if (out === '' || into === '' || out === into || !Number.isInteger(minute) || minute <= 0 || minute >= duration) {
            message.textContent = 'Zgjidhni dy lojtarë të ndryshëm dhe minutën brenda ndeshjes.'; return;
        }
        const outgoing = rows[Number(out)], incoming = rows[Number(into)];
        const active = [...outgoing.querySelectorAll('.match-stint')].find(stint => {
            const [a,b] = stint.querySelectorAll('input[type=number]');
            return a.value !== '' && b.value !== '' && Number(a.value) < minute && Number(b.value) >= minute;
        });
        if (!active) { message.textContent = 'Lojtari që del duhet të jetë në lojë në këtë minutë.'; return; }
        if ([...incoming.querySelectorAll('.match-stint')].some(stint => {
            const [a,b] = stint.querySelectorAll('input[type=number]'); return a.value === '' || b.value === '' || Number(b.value) > minute;
        })) { message.textContent = 'Kontrolloni intervalet e lojtarit që hyn; ai ka një interval të paplotë ose të mëvonshëm.'; return; }
        active.querySelectorAll('input[type=number]')[1].value = minute;
        if (incoming.querySelector('.match-role').value !== 'Starter') incoming.querySelector('.match-role').value = 'Substitute';
        add(incoming);
        const inputs = incoming.querySelector('.match-stint:last-child').querySelectorAll('input[type=number]');
        inputs[0].value = minute; inputs[1].value = duration;
        dirty = true; update(); message.textContent = 'Zëvendësimi u vendos. Ruaj ndeshjen për ta konfirmuar.';
    });
    window.addEventListener('beforeunload', event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } });
    update();
})();
