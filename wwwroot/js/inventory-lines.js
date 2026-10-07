(() => {
    const issue = !!document.querySelector('#issue-form');
    const form = document.querySelector(issue ? '#issue-form' : '#stock-entry-form');
    const body = document.querySelector(issue ? '#issue-lines' : '#stock-lines');
    if (!form || !body?.rows.length) return;
    const catalog = JSON.parse(document.querySelector('#variant-catalog').textContent);
    const add = document.querySelector(issue ? '#add-line' : '#add-stock-line');
    const template = body.rows[0].cloneNode(true);
    const field = (row, name) => row.querySelector(`[data-line-field="${name}"]`);
    const cents = value => { const m = /^(\d+)(?:\.(\d{0,2}))?$/.exec(value); return m ? BigInt(m[1]) * 100n + BigInt((m[2] || '').padEnd(2, '0')) : 0n; };
    const money = value => `${value / 100n}.${(value % 100n).toString().padStart(2, '0')}`;
    let net = 0n;
    function sizes(row, preserve) {
        const select = field(row, 'ProductVariantId'), selected = preserve ? select.value : '';
        select.replaceChildren(new Option('Zgjidhni madhësinë', ''));
        catalog.filter(v => String(v.productId) === field(row, 'ProductId').value).forEach(v => {
            select.add(new Option(`${v.size} · stok ${v.stock} ${v.unit.toLowerCase()}`, v.id));
        });
        select.value = selected;
    }
    function update() {
        let gross = 0n;
        const totals = new Map();
        Array.from(body.rows).forEach(row => {
            const id = field(row, 'ProductVariantId').value;
            totals.set(id, (totals.get(id) || 0n) + cents(field(row, 'Quantity').value));
        });
        Array.from(body.rows).forEach((row, index) => {
            row.querySelectorAll('[data-line-field]').forEach(input => {
                input.name = `Lines[${index}].${input.dataset.lineField}`;
                input.id = `Lines_${index}__${input.dataset.lineField}`;
            });
            row.querySelectorAll('input[name="__Invariant"]').forEach(input => {
                input.value = input.value.replace(/^Lines\[\d+\]\./, `Lines[${index}].`);
            });
            const quantity = field(row, 'Quantity'), product = field(row, 'ProductId');
            const price = issue ? product.selectedOptions[0]?.dataset.price || '0' : field(row, 'UnitPrice').value;
            const value = (cents(quantity.value) * cents(price) + 50n) / 100n;
            gross += value; row.querySelector('.line-total').textContent = money(value) + (issue ? ' €' : '');
            const id = field(row, 'ProductVariantId').value, variant = catalog.find(v => String(v.id) === id);
            quantity.setCustomValidity(issue && variant && totals.get(id) > cents(String(variant.stock)) ? 'Sasia totale e kësaj madhësie tejkalon stokun.' : '');
            row.querySelector('.remove-line').disabled = body.rows.length === 1;
        });
        add.disabled = body.rows.length >= (issue ? 6 : 50);
        if (!issue) { document.querySelector('#stock-total').textContent = money(gross); return; }
        const paid = document.querySelector('#AmountPaid'), free = document.querySelector('#WaivePayment')?.checked === true;
        net = free ? 0n : gross;
        if (free) paid.value = '0';
        paid.readOnly = free;
        document.querySelector('#WaiverReason').required = free;
        document.querySelector('#Notes').required = free;
        document.querySelector('#waiver-fields').hidden = !free;
        document.querySelector('#note-required').hidden = !free;
        document.querySelector('#pay-full').disabled = free;
        document.querySelector('#gross').textContent = money(gross) + ' €';
        document.querySelector('#net').textContent = money(net) + ' €';
        const due = net - cents(paid.value);
        document.querySelector('#due').textContent = money(due > 0n ? due : 0n) + ' €';
        paid.setCustomValidity(cents(paid.value) > net ? 'Pagesa tejkalon detyrimin.' : '');
    }
    add.onclick = () => {
        if (body.rows.length >= (issue ? 6 : 50)) return;
        const row = template.cloneNode(true);
        row.querySelectorAll('[data-line-field]').forEach(input => input.value = input.dataset.lineField === 'Quantity' ? '1' : '');
        row.querySelectorAll('.input-validation-error').forEach(input => input.classList.remove('input-validation-error'));
        sizes(row, false); body.append(row); update(); field(row, 'ProductId').focus();
    };
    body.addEventListener('click', event => {
        if (event.target.closest('.remove-line') && body.rows.length > 1) { event.target.closest('tr').remove(); update(); }
    });
    form.addEventListener('input', update);
    form.addEventListener('change', event => {
        const row = event.target.closest('tr');
        if (event.target.dataset.lineField === 'ProductId') { sizes(row, false); if (!issue) field(row, 'UnitPrice').value = ''; }
        if (!issue && event.target.dataset.lineField === 'ProductVariantId') {
            const variant = catalog.find(v => String(v.id) === event.target.value);
            field(row, 'UnitPrice').value = variant ? String(variant.price) : '';
        }
        update();
    });
    if (issue) {
        document.querySelector('#pay-full').onclick = () => { document.querySelector('#AmountPaid').value = money(net); update(); };
    }
    Array.from(body.rows).forEach(row => sizes(row, true)); update();
})();
