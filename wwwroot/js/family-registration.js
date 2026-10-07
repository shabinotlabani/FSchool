(() => {
    const form=document.querySelector('#family-registration'),body=document.querySelector('#registration-children'),family=document.querySelector('#FamilyId');
    const pricing=JSON.parse(document.querySelector('#registration-pricing').textContent),template=body.children[0].cloneNode(true),requests=new WeakMap();
    const field=(row,name)=>row.querySelector(`[data-child-field="${name}"]`);
    document.querySelector('#FeePlanId').addEventListener('change',update);
    function update(){
        const planSelect=document.querySelector('#FeePlanId');
        document.querySelector('#family-tariff-choice').hidden=!!family.value;planSelect.disabled=!!family.value;
        const selected=pricing.plans.find(p=>p.id===Number(family.value?pricing.familyPlans[family.value]:planSelect.value));
        const existing=pricing.counts[family.value]||0;let total=0;
        [...body.children].forEach((row,index)=>{
            row.querySelector('.child-number').textContent=String(index+1);
            row.querySelectorAll('[data-child-field]').forEach(input=>{input.name=`Children[${index}].${input.dataset.childField}`;input.id=`Children_${index}__${input.dataset.childField}`;});
            row.querySelectorAll('[data-child-label]').forEach(label=>label.htmlFor=`Children_${index}__${label.dataset.childLabel}`);
            row.querySelectorAll('input[name="__Invariant"]').forEach(input=>input.value=input.value.replace(/^Children\[\d+\]\./,`Children[${index}].`));
            const amount=selected?(existing+index<selected.limit?selected.first:selected.additional):0;
            const manual=row.querySelector('.child-manual').checked, price=field(row,'FirstMonthAmount'), reason=field(row,'FirstMonthReason');
            price.disabled=!manual;price.required=manual;reason.disabled=!manual;reason.required=manual;
            row.querySelector('.child-fee-reason').hidden=!manual;
            if(!manual)price.value=Number(amount).toFixed(2);
            total+=Math.round((manual?(Number(price.value)||0):amount)*100);
            row.querySelector('.child-fee').textContent=Number(amount).toFixed(2)+' €';
            row.querySelector('.remove-child').disabled=body.children.length<=2;
        });
        document.querySelector('#registration-total').textContent=(total/100).toFixed(2);
        document.querySelector('#FamilyName').required=!family.value;
        document.querySelector('#family-name-field').hidden=!!family.value;
        document.querySelector('#add-child').disabled=body.children.length>=20;
    }
    document.querySelector('#add-child').onclick=()=>{
        if(body.children.length>=20)return;const row=template.cloneNode(true);
        row.querySelectorAll('[data-child-field]').forEach(input=>input.value='');
        row.querySelector('.child-manual').checked=false;
        field(row,'LastName').value=field(body.children[0],'LastName').value;
        field(row,'TrainingTeamId').replaceChildren(new Option('Pa ekip — caktoje më vonë',''));
        row.querySelector('.team-message').textContent='Vendosni datëlindjen për ekipet dhe oraret.';
        row.querySelectorAll('.input-validation-error').forEach(input=>input.classList.remove('input-validation-error'));
        body.append(row);update();field(row,'FirstName').focus();
    };
    body.addEventListener('click',e=>{const button=e.target.closest('.remove-child');if(button&&body.children.length>2){const row=button.closest('.registration-child');requests.get(row)?.abort();row.remove();update();}});
    body.addEventListener('input',e=>{if(e.target.dataset.childField==='FirstMonthAmount')update();});
    body.addEventListener('change',async e=>{
        if(e.target.classList.contains('child-manual')){update();return;}
        if(e.target.dataset.childField!=='DateOfBirth')return;const row=e.target.closest('.registration-child'),select=field(row,'TrainingTeamId'),status=row.querySelector('.team-message');
        requests.get(row)?.abort();const request=new AbortController();requests.set(row,request);const selected=select.value;
        if(!e.target.value){select.replaceChildren(new Option('Pa ekip — caktoje më vonë',''));status.textContent='Vendosni datëlindjen.';return;}
        status.textContent='Duke kërkuar ekipet…';
        try{
            const response=await fetch(form.dataset.teamsUrl+'?dateOfBirth='+encodeURIComponent(e.target.value),{signal:request.signal,cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();if(request.signal.aborted)return;
            select.replaceChildren(new Option('Pa ekip — caktoje më vonë',''));
            data.teams.forEach(team=>{const times=team.sessions.map(s=>`${s.day} ${s.start}–${s.end}`).join('; ');const option=new Option(`${team.name} · ${team.freePlaces} vende · ${times}`,team.id);option.disabled=team.freePlaces===0;select.add(option);});
            if([...select.options].some(option=>option.value===selected&&!option.disabled))select.value=selected;
            status.textContent=`Mosha: ${data.age} vjeç. Vendet konfirmohen për të gjithë fëmijët gjatë ruajtjes.`;
        }catch(error){if(error.name!=='AbortError')status.textContent='Ekipet nuk u ngarkuan. Kontrolloni datëlindjen dhe provoni përsëri.';}
    });family.addEventListener('change',update);update();
})();
