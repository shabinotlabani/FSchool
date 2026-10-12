(() => {
    const form=document.querySelector('.payment-form'),dialog=document.querySelector('#payment-confirm');
    const yes=document.querySelector('#payment-confirm-yes');
    let approved=false;
    form.addEventListener('submit',event=>{
        if(approved){yes.disabled=true;form.querySelector('button[type=submit]').disabled=true;return;}
        event.preventDefault();
        const amount=Number(form.querySelector('#Amount').value).toLocaleString('sq-AL',{minimumFractionDigits:2,maximumFractionDigits:2});
        const method=form.querySelector('[name=PaymentMethod]:checked')?.value==='Bank'?'Bankë':'Cash';
        document.querySelector('#payment-confirm-value').textContent=amount+' · '+method;
        document.querySelector('#payment-confirm-date').textContent=form.querySelector('#PaymentDate').value.split('-').reverse().join('.');
        dialog.showModal();
    });
    document.querySelector('#payment-confirm-no').addEventListener('click',()=>dialog.close());
    yes.addEventListener('click',()=>{if(approved)return;approved=true;dialog.close();form.requestSubmit();approved=false;});
})();
