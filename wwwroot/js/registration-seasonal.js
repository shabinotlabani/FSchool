(() => {
    const root=document.querySelector('#registration-payment');
    if(!root)return;
    const pricing=JSON.parse(document.querySelector('#first-month-pricing').textContent);
    const plan=document.querySelector('#FeePlanId'), kind=document.querySelector('#RegistrationPayment');
    const months=document.querySelector('#SeasonalMonths'), method=document.querySelector('#SeasonalPaymentMethod');
    const monthly=document.querySelector('#registration-monthly');
    function update(){
        const tariff=pricing.plans.find(p=>String(p.id)===plan.value);
        const allowed=tariff&&!tariff.isFamily&&tariff.amount>0&&root.dataset.familyPicker!=='true';
        kind.querySelector('[value="Seasonal"]').disabled=!allowed;
        if(!allowed)kind.value='Monthly';
        const seasonal=kind.value==='Seasonal';
        root.closest('form').querySelector('button[type="submit"]').textContent=seasonal?(method.value==='Bank'?'Regjistro dhe dërgo në bankë':'Regjistro dhe paguaj sezonin'):'Regjistro lojtarin';
        monthly.hidden=seasonal;monthly.disabled=seasonal;
        document.querySelector('#registration-seasonal').hidden=!seasonal;
        document.querySelector('#registration-season-length').hidden=!seasonal;
        document.querySelector('#SeasonalExpectedRate').value=Number(tariff?.amount||0).toFixed(2);
        if(!seasonal)return;
        const duration=Number(months.value), paid=duration-duration/6, rate=Number(tariff.amount);
        const period=document.querySelector('#registration-season-period');
        const [year,month]=period.dataset.start.split('-').map(Number);
        const end=new Date(year,month-1+duration-1,1);
        period.textContent=`${String(month).padStart(2,'0')}.${year} – ${String(end.getMonth()+1).padStart(2,'0')}.${end.getFullYear()}`;
        document.querySelector('#registration-season-total').textContent=`Gjithsej: ${(rate*paid).toFixed(2)}`;
        document.querySelector('#registration-season-calculation').textContent=`${paid} × ${rate.toFixed(2)} · ${duration/6} muaj falas`;
        document.querySelector('#registration-season-bank').hidden=method.value!=='Bank';
    }
    [plan,kind,months,method].forEach(el=>el.addEventListener('change',update));
    update();
})();
