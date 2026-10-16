(() => {
    const dialog = document.getElementById('financial-confirm');
    const yes = document.getElementById('financial-confirm-yes');
    let pending = null;
    let approved = null;
    const submitted = new WeakSet();

    // Bubble after form validation handlers; retain the clicked button's name/value.
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!form.matches('form[data-financial-confirm]') || form.method.toLowerCase() !== 'post') return;
        if (submitted.has(form)) { event.preventDefault(); return; }
        if (event.defaultPrevented) return;
        if (approved === form) { submitted.add(form); return; }
        event.preventDefault();
        if (pending) return;
        pending = { form, submitter: event.submitter, focus: document.activeElement };
        document.getElementById('financial-confirm-action').textContent =
            event.submitter?.textContent.trim() || document.querySelector('h1')?.textContent || 'Ruaj veprimin';
        const details = [];
        const amount = form.querySelector('[name="Amount"], [name="ReceivedAmount"]');
        if (amount?.value && Number.isFinite(Number(amount.value))) {
            details.push('Shuma: ' + Number(amount.value).toLocaleString('sq-AL', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        }
        const method = form.querySelector('select[name="PaymentMethod"], input[name="PaymentMethod"]:checked');
        if (method) details.push(method.selectedOptions?.[0]?.textContent || (method.value === 'Bank' ? 'Bankë' : method.value));
        const date = form.querySelector('[name="PaymentDate"], [name="ReceivedOn"]');
        if (date?.value) details.push(date.value.split('-').reverse().join('.'));
        document.getElementById('financial-confirm-details').textContent = details.join(' · ');
        yes.disabled = false;
        dialog.showModal();
    });

    function dismiss() {
        const focus = pending?.focus;
        pending = null;
        dialog.close();
        focus?.focus();
    }
    document.getElementById('financial-confirm-no').addEventListener('click', dismiss);
    dialog.addEventListener('cancel', event => { event.preventDefault(); dismiss(); });
    yes.addEventListener('click', () => {
        if (!pending || yes.disabled) return;
        const { form, submitter } = pending;
        yes.disabled = true;
        dismiss();
        approved = form;
        try {
            if (submitter) form.requestSubmit(submitter);
            else form.requestSubmit();
        } finally { approved = null; }
    });
    window.addEventListener('pageshow', () => {
        document.querySelectorAll('form[data-financial-confirm]').forEach(form => submitted.delete(form));
    });
})();
