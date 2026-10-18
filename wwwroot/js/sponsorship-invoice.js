(() => {
    const form = document.getElementById('sponsor-invoice');
    if (!form) return;
    const category = form.elements.PlanLineId, date = form.elements.Date, amount = form.elements.Amount;
    const options = document.getElementById('budget-options'), save = document.getElementById('save-invoice');
    const retry = document.getElementById('retry-budgets'), salary = document.getElementById('invoice-salary');
    const money = new Intl.NumberFormat('sq-AL', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    let timer, request, version = 0;
    function element(tag, text, className) {
        const el = document.createElement(tag); if (text !== undefined) el.textContent = text;
        if (className) el.className = className; return el;
    }
    function message(text) { options.replaceChildren(element('p', text, 'sp-budget-empty')); }
    function salaryField() {
        const isSalary = category.selectedOptions[0]?.dataset.salary === 'true';
        form.elements.SalaryRole.disabled = !isSalary; form.elements.SalaryRole.required = isSalary;
        salary.hidden = !isSalary; form.elements.SalaryMonth.disabled = !isSalary; form.elements.SalaryMonth.required = isSalary;
    }
    async function refresh(preferred) {
        const current = ++version; request?.abort(); request = new AbortController();
        save.disabled = true; retry.hidden = true; salaryField();
        if (!category.value || !date.value || !amount.value || !amount.checkValidity() || Number(amount.value) <= 0) {
            message('Zgjidhni kategorinë dhe shumën.'); return;
        }
        message('Duke kontrolluar buxhetet…');
        const url = new URL(form.dataset.budgetUrl, window.location.origin);
        url.search = new URLSearchParams({ planLineId: category.value, date: date.value, amount: amount.value });
        try {
            const response = await fetch(url, { signal: request.signal, headers: { Accept: 'application/json' } });
            const result = await response.json(); if (version !== current) return;
            if (!response.ok) { message(result.message || 'Buxheti nuk mund të propozohet.'); return; }
            options.replaceChildren();
            if (!result.length) { message('Nuk ka buxhet të vlefshëm për këtë kategori dhe datë.'); return; }
            const recommended = result.find(x => x.canCover);
            const selected = result.find(x => x.canCover && String(x.allocationId) === preferred) || recommended;
            const wrapper = element('div', undefined, 'sp-table-wrap'); wrapper.tabIndex = 0;
            wrapper.setAttribute('role', 'region'); wrapper.setAttribute('aria-label', 'Buxhetet e propozuara');
            const table = element('table', undefined, 'table sp-table sp-budget-table');
            const head = element('thead'), header = element('tr'), body = element('tbody');
            ['Zgjidh', 'Sponsori / kontrata', 'Kategoria', 'Në dispozicion', 'Nga ky buxhet', 'Deri më', 'Statusi'].forEach((text, i) => {
                const cell = element('th', text, i === 3 || i === 4 ? 'sp-number' : undefined); cell.scope = 'col'; header.append(cell);
            });
            head.append(header); table.append(head, body); wrapper.append(table); options.append(wrapper);
            for (const item of result) {
                const row = element('tr', undefined, item.canCover ? undefined : 'sp-budget-unavailable');
                const radio = document.createElement('input'); radio.type = 'radio'; radio.name = 'AllocationId';
                radio.value = item.allocationId; radio.required = true; radio.disabled = !item.canCover;
                radio.id = 'budget-' + item.allocationId;
                radio.checked = selected?.allocationId === item.allocationId;
                const choice = element('td'); choice.append(radio);
                const sponsor = element('th'); sponsor.scope = 'row';
                const label = element('label', item.sponsor); label.htmlFor = radio.id;
                sponsor.append(label, element('small', item.reference, 'd-block'));
                const status = item.canCover ? (recommended?.allocationId === item.allocationId ? 'I propozuar' : 'I disponueshëm') : 'Nuk mjafton';
                row.append(choice, sponsor, element('td', item.category), element('td', money.format(item.available), 'sp-number'),
                    element('td', money.format(item.proposed), 'sp-number'), element('td', item.expires.split('-').reverse().join('.'), 'sp-nowrap'), element('td', status));
                body.append(row);
            }
            save.disabled = !selected;
        } catch (error) {
            if (error.name === 'AbortError' || version !== current) return;
            message('Nuk u lexuan buxhetet.'); retry.hidden = false;
        }
    }
    for (const input of [category, date, amount]) input.addEventListener('input', () => {
        const selected = form.querySelector('[name="AllocationId"]:checked')?.value;
        clearTimeout(timer); ++version; request?.abort(); save.disabled = true;
        salaryField(); message('Duke kontrolluar buxhetet…');
        timer = setTimeout(() => refresh(selected), 250);
    });
    retry.addEventListener('click', () => refresh());
    refresh(form.querySelector('[name="AllocationId"]:checked')?.value);
})();
