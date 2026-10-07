(() => {
    const form=document.querySelector('#family-form'), body=document.querySelector('#family-members'), chooser=document.querySelector('#family-student');
    let pricing=JSON.parse(document.querySelector('#family-pricing').textContent), request;
    function update(){
        let total=0;
        [...body.rows].forEach((row,i)=>{
            const amount=body.rows.length===1&&pricing.singleStandard?pricing.standard:i<pricing.limit?pricing.first:pricing.additional;
            total+=Math.round(amount*100);row.querySelector('.position').textContent=String(i+1);row.querySelector('.price').textContent=Number(amount).toFixed(2)+' €';
            row.querySelector('[data-move="up"]').disabled=i===0;row.querySelector('[data-move="down"]').disabled=i===body.rows.length-1;
        });
        document.querySelector('#family-total').textContent=(total/100).toFixed(2);
        document.querySelector('#family-rule').textContent=`${pricing.limit} fëmijët e parë nga ${pricing.first} €; çdo fëmijë shtesë ${pricing.additional} €. Një fëmijë i vetëm: ${pricing.singleStandard?pricing.standard:pricing.first} €.`;
        for(const option of chooser.options)option.disabled=[...body.querySelectorAll('input')].some(input=>input.value===option.value);
        document.querySelector('#family-add').disabled=body.rows.length>=20;
    }
    document.querySelector('#family-add').onclick=()=>{
        const option=chooser.selectedOptions[0];if(!option?.value||option.disabled||body.rows.length>=20)return;
        const row=document.createElement('tr');
        row.innerHTML='<td class="position"></td><td><input type="hidden" name="StudentIds"/><span></span></td><td class="price"></td><td class="text-nowrap"><button type="button" class="btn btn-sm btn-outline-secondary" data-move="up" aria-label="Lëviz lart">↑</button> <button type="button" class="btn btn-sm btn-outline-secondary" data-move="down" aria-label="Lëviz poshtë">↓</button> <button type="button" class="btn btn-sm btn-outline-danger" data-move="remove">Hiq</button></td>';
        row.querySelector('input').value=option.value;row.querySelector('span').textContent=option.text;body.append(row);chooser.value='';update();
    };
    body.addEventListener('click',e=>{const button=e.target.closest('[data-move]');if(!button)return;const row=button.closest('tr');if(button.dataset.move==='remove')row.remove();else if(button.dataset.move==='up'&&row.previousElementSibling)body.insertBefore(row,row.previousElementSibling);else if(button.dataset.move==='down'&&row.nextElementSibling)body.insertBefore(row.nextElementSibling,row);update();});
    document.querySelector('#family-search').addEventListener('input',e=>{const terms=e.target.value.toLocaleLowerCase().trim().split(/\s+/);for(const option of chooser.options)option.hidden=option.value!==''&&!terms.every(t=>option.text.toLocaleLowerCase().includes(t));});
    async function refreshPricing(){
        request?.abort();request=new AbortController();const signal=request.signal;
        try{const response=await fetch(form.dataset.pricingUrl+'?month='+encodeURIComponent(document.querySelector('#EffectiveMonth').value)+'&planId='+encodeURIComponent(document.querySelector('#FeePlanId').value),{signal,cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();if(signal.aborted)return;pricing=data;update();}
        catch(error){if(error.name!=='AbortError')document.querySelector('#family-rule').textContent='Çmimet nuk u ngarkuan. Rihapni formularin për muajin e zgjedhur.';}
    }
    document.querySelector('#EffectiveMonth').addEventListener('change',refreshPricing);
    document.querySelector('#FeePlanId').addEventListener('change',refreshPricing);update();
})();
