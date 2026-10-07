(() => {
    const tariff=document.querySelector('#PersonalTariffId'), body=document.querySelector('#sessions'), add=document.querySelector('#add-session');
    function update(changed) {
        const selected=tariff.selectedOptions[0], monthly=selected?.dataset.mode==='Monthly';
        if(changed)document.querySelector('#ExpectedRate').value=selected?.dataset.price??'0';
        add.hidden=!monthly;
        if(!monthly)while(body.rows.length>1)body.rows[body.rows.length-1].remove();
        for(const row of body.rows){row.cells[0].hidden=!monthly;row.querySelector('.remove-session').hidden=!monthly;}
        body.closest('table').querySelector('th').hidden=!monthly;
        add.disabled=body.rows.length>=14;
    }
    tariff.addEventListener('change',()=>update(true));
    update(false);
})();