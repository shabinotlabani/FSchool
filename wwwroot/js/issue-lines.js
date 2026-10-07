(() => {
    'use strict';
    const form = document.querySelector('#issue-form');
    if (!form) return;
    const body = document.querySelector('#issue-lines');
    const add = document.querySelector('#add-line');
    const products = JSON.parse(document.querySelector('#issue-product-catalog').textContent);
    const variants = JSON.parse(document.querySelector('#variant-catalog').textContent);
    const template = body.querySelector('.issue-line').cloneNode(true);
    const rows = () => Array.from(body.querySelectorAll('.issue-line'));
    const field = (row, name) => row.querySelector(`[data-line-field="${name}"]`);
    const normalize = value => String(value ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim();
    const parse = value => {
        const match = /^(\d+)(?:\.(\d{0,2}))?$/.exec(String(value).replace(/^\./, '0.'));
        return match ? BigInt(match[1]) * 100n + BigInt((match[2] || '').padEnd(2, '0')) : null;
    };
    const cents = value => parse(value) ?? 0n;
    const money = value => `${value / 100n}.${(value % 100n).toString().padStart(2, '0')}`;
    const selectedProduct = row => products.find(p => String(p.id) === field(row, 'ProductId').value);
    const productVariants = product => variants.filter(v => v.productId === product.id);
    const stockLabel = product => productVariants(product).map(v => `${v.size}: ${v.stock}`).join(' · ');
    const termsFor = product => normalize(`${product.name} ${product.code} ${productVariants(product).map(v => v.size).join(' ')}`);
    let net = 0n;
    // Preserve amounts returned after server validation, including a deliberate zero.
    let automaticPayment = form.dataset.autoPayment === 'true';

    function sizes(row, preserve) {
        const select = field(row, 'ProductVariantId');
        const previous = preserve ? select.value : '';
        select.replaceChildren(new Option('Zgjidhni madhësinë', ''));
        const product = selectedProduct(row);
        const available = product ? productVariants(product) : [];
        for (const variant of available) {
            const option = new Option(`${variant.size} · stok ${variant.stock} ${variant.unit.toLowerCase()}`, variant.id);
            option.disabled = Number(variant.stock) <= 0;
            select.add(option);
        }
        select.value = previous;
        // Standard-size articles need no extra click; otherwise choose explicitly.
        if (!select.value && available.length === 1 && Number(available[0].stock) > 0) select.value = String(available[0].id);
    }

    function showSelection(row, product, preservePrice) {
        field(row, 'ProductId').value = String(product.id);
        const search = row.querySelector('.product-search');
        search.required = false;
        search.setCustomValidity('');
        row.querySelector('.product-search-area').hidden = true;
        row.querySelector('.selected-product').hidden = false;
        row.querySelector('.selected-product-name').textContent = product.name;
        row.querySelector('.selected-product-info').textContent = `${product.unit} · Stoku sipas madhësisë: ${stockLabel(product) || 'Pa madhësi aktive'}`;
        row.querySelector('.product-search-results').replaceChildren();
        const price = field(row, 'UnitPrice');
        if (!preservePrice || !price.value && !price.classList.contains('input-validation-error')) price.value = product.price;
        sizes(row, preservePrice);
    }

    function clearSelection(row) {
        field(row, 'ProductId').value = '';
        field(row, 'UnitPrice').value = '';
        const search = row.querySelector('.product-search');
        search.value = '';
        search.required = true;
        search.setCustomValidity('');
        row.querySelector('.product-search-area').hidden = false;
        row.querySelector('.selected-product').hidden = true;
        row.querySelector('.product-search-results').replaceChildren();
        row.querySelector('.product-search-status').textContent = 'Shkruani për të kërkuar rekuizitën.';
        sizes(row, false);
    }

    function searchProducts(row) {
        const search = row.querySelector('.product-search');
        search.setCustomValidity('');
        const results = row.querySelector('.product-search-results');
        const status = row.querySelector('.product-search-status');
        results.replaceChildren();
        const terms = normalize(search.value).split(/\s+/).filter(Boolean);
        if (!terms.length) { status.textContent = 'Shkruani për të kërkuar rekuizitën.'; return; }
        const matches = products.filter(p => terms.every(t => termsFor(p).includes(t)));
        status.textContent = !matches.length ? 'Nuk u gjet rekuizitë. Provoni një emër ose madhësi tjetër.'
            : matches.length > 6 ? `${matches.length} rekuizita të gjetura. Shfaqen 6 të parat; ngushtoni kërkimin.` : `${matches.length} rekuizita të gjetura. Klikoni për të zgjedhur.`;
        for (const product of matches.slice(0, 6)) {
            const li = document.createElement('li'), button = document.createElement('button');
            button.type = 'button'; button.className = 'player-search-result pick-product'; button.dataset.productId = String(product.id);
            const info = document.createElement('span'), name = document.createElement('strong'), details = document.createElement('small');
            name.textContent = product.name;
            details.textContent = `${product.price} € / ${product.unit} · ${stockLabel(product) || 'Pa madhësi aktive'}`;
            info.append(name, details);
            const label = document.createElement('span'); label.className = 'player-pick-label';
            button.disabled = !productVariants(product).some(v => Number(v.stock) > 0);
            label.textContent = button.disabled ? 'Pa stok' : 'Zgjidh →';
            button.append(info, label); li.append(button); results.append(li);
        }
    }

    function update() {
        let gross = 0n;
        const lines = rows(), totals = new Map();
        const free = document.querySelector('#WaivePayment')?.checked === true;
        for (const row of lines) {
            const id = field(row, 'ProductVariantId').value;
            totals.set(id, (totals.get(id) || 0n) + cents(field(row, 'Quantity').value));
        }
        lines.forEach((row, index) => {
            row.querySelector('.line-number').textContent = String(index + 1);
            row.querySelectorAll('[data-line-field]').forEach(input => {
                input.name = `Lines[${index}].${input.dataset.lineField}`;
                input.id = `Lines_${index}__${input.dataset.lineField}`;
            });
            row.querySelectorAll('[data-line-label]').forEach(label => label.htmlFor = `Lines_${index}__${label.dataset.lineLabel}`);
            row.querySelectorAll('input[name="__Invariant"]').forEach(input => input.value = input.value.replace(/^Lines\[\d+\]\./, `Lines[${index}].`));
            const search = row.querySelector('.product-search');
            search.id = `issue-product-search-${index}`;
            search.setAttribute('aria-describedby', `issue-product-status-${index}`);
            row.querySelector('.product-search-label').htmlFor = search.id;
            row.querySelector('.product-search-status').id = `issue-product-status-${index}`;
            const quantity = field(row, 'Quantity'), price = field(row, 'UnitPrice');
            const value = (cents(quantity.value) * cents(price.value) + 50n) / 100n;
            gross += value;
            row.querySelector('.line-total').textContent = money(value) + ' €';
            const product = selectedProduct(row), variant = variants.find(v => String(v.id) === field(row, 'ProductVariantId').value);
            quantity.setCustomValidity(variant && totals.get(String(variant.id)) > cents(variant.stock) ? 'Sasia totale e kësaj madhësie tejkalon stokun.' : '');
            price.setCustomValidity(price.value && parse(price.value) === null ? 'Shkruani çmimin me deri në dy shifra dhjetore.' : price.value && cents(price.value) === 0n && !free ? 'Për çmim zero përdorni lirimin me arsye dhe shënim.' : '');
            row.querySelector('.list-price-hint').textContent = product ? `Çmimi i listës: ${product.price} €` : '';
            row.querySelector('.reset-price').hidden = !product || parse(price.value) === cents(product.price);
            row.querySelector('.remove-line').disabled = lines.length === 1;
        });
        add.disabled = lines.length >= 6;
        const paid = document.querySelector('#AmountPaid');
        net = free ? 0n : gross;
        if (free) paid.value = '0';
        else if (automaticPayment) paid.value = money(net);
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

    add.addEventListener('click', () => {
        if (rows().length >= 6) return;
        const row = template.cloneNode(true);
        row.querySelectorAll('[data-line-field]').forEach(input => input.value = input.dataset.lineField === 'Quantity' ? '1' : '');
        row.querySelectorAll('.input-validation-error').forEach(input => input.classList.remove('input-validation-error'));
        body.append(row); clearSelection(row); update(); row.querySelector('.product-search').focus();
    });
    body.addEventListener('click', event => {
        const row = event.target.closest('.issue-line');
        if (!row) return;
        const pick = event.target.closest('.pick-product');
        if (pick && !pick.disabled) {
            const product = products.find(p => String(p.id) === pick.dataset.productId);
            if (product) { showSelection(row, product, false); update(); field(row, 'ProductVariantId').focus(); }
        } else if (event.target.closest('.change-product')) { clearSelection(row); update(); row.querySelector('.product-search').focus(); }
        else if (event.target.closest('.reset-price')) { const product = selectedProduct(row); if (product) field(row, 'UnitPrice').value = product.price; update(); }
        else if (event.target.closest('.remove-line') && rows().length > 1) { row.remove(); update(); add.focus(); }
    });
    form.addEventListener('input', event => {
        if (event.target.id === 'AmountPaid') automaticPayment = false;
        if (event.target.classList.contains('product-search')) searchProducts(event.target.closest('.issue-line'));
        update();
    });
    form.addEventListener('change', event => {
        if (event.target.id === 'AmountPaid') automaticPayment = false;
        update();
    });
    body.addEventListener('keydown', event => {
        const row = event.target.closest('.issue-line');
        if (!row) return;
        const buttons = Array.from(row.querySelectorAll('.pick-product')).filter(b => !b.disabled);
        if (event.target.classList.contains('product-search') && (event.key === 'Enter' || event.key === 'ArrowDown')) { event.preventDefault(); buttons[0]?.focus(); }
        else if (buttons.includes(event.target) && ['ArrowUp', 'ArrowDown', 'Escape'].includes(event.key)) {
            event.preventDefault();
            const index = buttons.indexOf(event.target) + (event.key === 'ArrowDown' ? 1 : -1);
            if (event.key === 'Escape' || index < 0) row.querySelector('.product-search').focus();
            else buttons[Math.min(index, buttons.length - 1)].focus();
        }
    });
    form.addEventListener('submit', event => {
        update();
        for (const row of rows()) if (!selectedProduct(row)) {
            event.preventDefault(); const search = row.querySelector('.product-search');
            search.setCustomValidity('Zgjidhni rekuizitën nga rezultatet.'); search.reportValidity(); search.focus(); return;
        }
        if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); }
    });
    document.querySelector('#pay-full').addEventListener('click', () => { automaticPayment = true; update(); });
    for (const row of rows()) { const product = selectedProduct(row); if (product) showSelection(row, product, true); else clearSelection(row); }
    update();
})();
