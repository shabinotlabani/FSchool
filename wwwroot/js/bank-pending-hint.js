(() => {
    const controls=[...document.querySelectorAll('[name=PaymentMethod], [name=Method]')];
    const hint=document.querySelector('.bank-pending-hint');
    if(!controls.length||!hint)return;
    const form=controls[0].form,submit=form.querySelector('button[type=submit], button:not([type])');
    const original=submit?.textContent;
    function update(){const bank=controls.some(x=>(x.tagName==='SELECT'||x.checked)&&x.value==='Bank');const automatic=bank&&form.dataset.automaticBank==='true';hint.hidden=!bank||automatic;if(submit){submit.hidden=automatic;submit.textContent=bank?'Ruaj në pritje të bankës':original;}}
    controls.forEach(x=>x.addEventListener('change',update));update();
})();
