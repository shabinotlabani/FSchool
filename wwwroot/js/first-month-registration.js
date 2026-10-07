(() => {
    const pricing=JSON.parse(document.querySelector('#first-month-pricing').textContent);
    const plan=document.querySelector('#FeePlanId'), family=document.querySelector('#FamilyId'), manual=document.querySelector('#manual-first-month');
    const amount=document.querySelector('#FirstMonthAmount'), reason=document.querySelector('#FirstMonthReason'), note=document.querySelector('#first-month-proposal');
    function update(){
        const selected=pricing.plans.find(p=>String(p.id)===plan.value);
        if(!selected)return;
        for (const option of family.options) {
            if (option.value) option.hidden = option.dataset.plan !== plan.value;
        }
        if (family.selectedOptions[0]?.hidden) family.value = '';
        let rate=selected.amount;
        const waiting=selected.isFamily&&!family.value;
        if(selected.isFamily&&family.value){
            const count=pricing.counts[family.value]||0;
            rate=count<selected.limit?selected.amount:selected.additional;
            if(count===0&&selected.singleStandard)rate=pricing.standard;
        }
        const proposed=Math.min(rate,Math.ceil(Math.round(rate*100)*pricing.weeks/400));
        const free=plan.value==='3';
        if(free)manual.checked=false;
        manual.disabled=free||waiting;
        amount.disabled=!manual.checked||free||waiting;
        amount.required=!amount.disabled;
        reason.disabled=amount.disabled;reason.required=!amount.disabled;
        document.querySelector('#first-month-reason').hidden=amount.disabled;
        if(amount.disabled)amount.value=proposed.toFixed(2);
        note.textContent=waiting?'Zgjidhni familjen për të parë propozimin.':'Propozimi për muajin e parë: '+proposed.toFixed(2)+' € · Tarifa normale: '+Number(rate).toFixed(2)+' € / muaj.';
    }
    plan.addEventListener('change',update);family.addEventListener('change',update);manual.addEventListener('change',update);update();
})();
