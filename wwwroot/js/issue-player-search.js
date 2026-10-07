(() => {
    'use strict';
    const form = document.querySelector('#issue-form');
    if (!form) return;
    const id = document.querySelector('#StudentId');
    const search = document.querySelector('#player-search');
    const area = document.querySelector('#player-search-area');
    const results = document.querySelector('#player-search-results');
    const status = document.querySelector('#player-search-status');
    const selected = document.querySelector('#selected-player');
    const change = document.querySelector('#change-player');
    const normalize = value => String(value ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim();
    const players = JSON.parse(document.querySelector('#player-catalog').textContent).map(player => ({
        ...player, searchable: normalize(`${player.name} ${player.parent ?? ''} ${player.phone ?? ''} ${(player.phone ?? '').replace(/\D/g, '')} #${player.id}`)
    }));
    const details = player => [`Datëlindja: ${player.birthDate}`, player.parent ? `Prindi: ${player.parent}` : '', player.phone, `#${player.id}`].filter(Boolean).join(' · ');

    function choose(player, focus = true) {
        id.value = String(player.id);
        search.setCustomValidity('');
        search.required = false;
        area.hidden = true;
        selected.hidden = false;
        document.querySelector('#selected-player-name').textContent = player.name;
        document.querySelector('#selected-player-details').textContent = details(player);
        results.replaceChildren();
        if (focus) change.focus();
    }

    function render() {
        results.replaceChildren();
        search.setCustomValidity('');
        const terms = normalize(search.value).split(/\s+/).filter(Boolean);
        if (!terms.length) {
            status.textContent = 'Shkruani për të kërkuar, pastaj klikoni lojtarin.';
            return;
        }
        const matches = players.filter(player => terms.every(term => player.searchable.includes(term)));
        status.textContent = !matches.length ? 'Nuk u gjet asnjë lojtar aktiv. Provoni emrin, mbiemrin ose telefonin.'
            : matches.length > 10 ? `U gjetën ${matches.length} lojtarë. Shfaqen 10 të parët; shkruani më shumë për ta ngushtuar kërkimin.`
                : `${matches.length} lojtarë të gjetur. Klikoni lojtarin për ta zgjedhur.`;
        for (const player of matches.slice(0, 10)) {
            const item = document.createElement('li');
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'player-search-result';
            const info = document.createElement('span');
            const name = document.createElement('strong');
            name.textContent = player.name;
            const detail = document.createElement('small');
            detail.textContent = details(player);
            info.append(name, detail);
            const label = document.createElement('span');
            label.className = 'player-pick-label';
            label.textContent = 'Zgjidh →';
            button.append(info, label);
            button.addEventListener('click', () => choose(player));
            item.append(button);
            results.append(item);
        }
    }

    search.addEventListener('input', render);
    search.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === 'ArrowDown') {
            event.preventDefault();
            results.querySelector('button')?.focus();
        }
    });
    results.addEventListener('keydown', event => {
        const buttons = Array.from(results.querySelectorAll('button'));
        const index = buttons.indexOf(event.target);
        if (index < 0) return;
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            const next = index + (event.key === 'ArrowDown' ? 1 : -1);
            if (next < 0) search.focus();
            else buttons[Math.min(next, buttons.length - 1)].focus();
        } else if (event.key === 'Escape') { event.preventDefault(); search.focus(); }
    });
    change.addEventListener('click', () => {
        id.value = '0';
        selected.hidden = true;
        area.hidden = false;
        search.value = '';
        search.required = true;
        render();
        search.focus();
    });
    form.addEventListener('submit', event => {
        if (!players.some(player => String(player.id) === id.value) || !area.hidden) {
            event.preventDefault();
            search.setCustomValidity('Klikoni një lojtar nga rezultatet përpara ruajtjes së daljes.');
            search.reportValidity();
            search.focus();
        }
    });
    const initial = players.find(player => String(player.id) === id.value);
    if (initial) choose(initial, false);
    else { id.value = '0'; render(); }
})();
