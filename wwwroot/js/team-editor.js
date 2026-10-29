(() => {
    const body = document.querySelector('#sessions');
    const add = document.querySelector('#add-session');
    if (body?.rows.length && add) {
    const template = body.rows[0].cloneNode(true);

    function update() {
        Array.from(body.rows).forEach((row, index) => {
            row.querySelectorAll('[data-session-field]').forEach(field => {
                const name = field.dataset.sessionField;
                field.name = `Sessions[${index}].${name}`;
                field.id = `Sessions_${index}__${name}`;
            });
            // Razor's time inputs include invariant-culture markers. Keep their
            // names intact and point their values at the reindexed time fields.
            row.querySelectorAll('input[name="__Invariant"]').forEach(marker => {
                marker.value = marker.value.replace(/^Sessions\[\d+\]\./, `Sessions[${index}].`);
            });
            row.querySelector('.remove-session').disabled = body.rows.length === 1;
        });
        add.disabled = body.rows.length >= 14;
    }

    add.onclick = () => {
        if (body.rows.length >= 14) return;
        const row = template.cloneNode(true);
        row.querySelectorAll('input[data-session-field]').forEach(field => field.value = '');
        body.append(row);
        update();
    };
    body.addEventListener('click', event => {
        if (event.target.closest('.remove-session') && body.rows.length > 1) {
            event.target.closest('tr').remove();
            update();
        }
    });
    update();
    }

    const assistantList = document.querySelector('#assistant-list');
    const assistantTemplate = document.querySelector('#assistant-template');
    const addAssistant = document.querySelector('#add-assistant');
    if (!assistantList || !assistantTemplate || !addAssistant) return;

    const refreshAssistants = () => {
        const rows = [...assistantList.querySelectorAll('.assistant-row')];
        const chosen = new Set(rows.map(row => row.querySelector('select')?.value).filter(Boolean));
        rows.forEach(row => {
            const select = row.querySelector('select');
            [...select.options].forEach(option => {
                option.disabled = option.value !== '' && option.value !== select.value && chosen.has(option.value);
            });
        });
        addAssistant.disabled = rows.length >= Math.max(0, assistantTemplate.content.querySelectorAll('option').length - 1);
    };
    addAssistant.addEventListener('click', () => {
        const row = assistantTemplate.content.firstElementChild.cloneNode(true);
        assistantList.append(row);
        row.querySelector('select').focus();
        refreshAssistants();
    });
    assistantList.addEventListener('click', event => {
        const remove = event.target.closest('.assistant-remove');
        if (!remove) return;
        remove.closest('.assistant-row').remove();
        refreshAssistants();
    });
    assistantList.addEventListener('change', refreshAssistants);
    refreshAssistants();
})();
