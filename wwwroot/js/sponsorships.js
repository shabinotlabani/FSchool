(() => {
    const lines = document.getElementById('plan-lines');
    function renumber() {
        lines?.querySelectorAll('[data-plan-line]').forEach((row, index) => {
            row.querySelector('[data-line-number]').textContent = index + 1;
            row.querySelectorAll('[name]').forEach(input => { input.name = input.name.replace(/Lines\[\d+\]/, `Lines[${index}]`); });
            // Existing control IDs remain stable while binding indexes follow row order.
        });
    }
    document.getElementById('add-plan-line')?.addEventListener('click', () => {
        if (lines.children.length >= 100) return;
        lines.append(document.getElementById('plan-line-template').content.cloneNode(true)); renumber();
        lines.lastElementChild.querySelector('input:not([type=hidden])').focus();
    });
    lines?.addEventListener('click', e => {
        if (!e.target.closest('[data-remove-line]') || lines.children.length <= 1) return;
        e.target.closest('[data-plan-line]').remove(); renumber();
    });
    const allocation = document.getElementById('sponsor-allocation');
    function salary() {
        const field = document.getElementById('salary-month-field');
        if (!field || !allocation) return;
        const enabled = allocation.selectedOptions[0]?.dataset.salary === 'true';
        field.hidden = !enabled;
        const role = field.querySelector('select'); role.required = enabled; role.disabled = !enabled;
        const input = field.querySelector('input'); input.required = enabled; input.disabled = !enabled;
    }
    allocation?.addEventListener('change', salary); salary();
    const contractTotal=document.querySelector('[data-contract-total]');
    const allocationInputs=[...document.querySelectorAll('[data-allocation-amount]')];
    function allocationTotals(){
        if(!contractTotal||!allocationInputs.length)return;
        const allocated=allocationInputs.reduce((sum,input)=>sum+(Number(input.value)||0),0);
        const contract=Number(contractTotal.value)||0;
        const total=document.querySelector('[data-allocation-total]');
        const difference=document.querySelector('[data-allocation-difference]');
        if(total)total.textContent=allocated.toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2});
        if(difference){
            const remaining=contract-allocated;
            difference.classList.toggle('sp-allocation-open',remaining>0);
            difference.classList.toggle('text-danger',remaining<0);
            difference.textContent=remaining===0?'✓ E ndarë plotësisht':remaining>0?`⚠ Pa u ndarë: ${remaining.toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2})}`:`Tejkalim: ${Math.abs(remaining).toLocaleString(undefined,{minimumFractionDigits:2,maximumFractionDigits:2})}`;
        }
    }
    [contractTotal,...allocationInputs].filter(Boolean).forEach(input=>input.addEventListener('input',allocationTotals));allocationTotals();
    document.getElementById('print-sponsorship')?.addEventListener('click', () => window.print());
})();
