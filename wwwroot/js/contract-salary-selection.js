(() => {
    const select = document.getElementById('contract-salary-basis');
    const values = document.getElementById('contract-salary-values');
    const salary = document.querySelector('.contract-summary-grid > div');
    if (!select || !values || !salary) return;
    function update() {
        const net = select.value === 'Net';
        salary.querySelector('span').textContent = net ? 'Paga neto aktuale' : 'Paga bruto aktuale';
        salary.querySelector('strong').textContent = net ? values.dataset.net : values.dataset.gross;
    }
    select.addEventListener('change', update);
    update();
})();
