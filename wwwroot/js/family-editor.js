(() => {
    const form=document.querySelector('#family-form'), body=document.querySelector('#family-members');
    const search=document.querySelector('#family-search'), results=document.querySelector('#family-results');
    const candidates=[...results.querySelectorAll('[data-student-id]')];
    const normalize=value=>value.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLocaleLowerCase();
    function filter(){
        const selected=new Set([...body.querySelectorAll('input[name="StudentIds"]')].map(input=>input.value));
        const terms=normalize(search.value).trim().split(/\s+/).filter(Boolean);
        if(!terms.length){results.hidden=true;document.querySelector('#family-search-status').textContent='';return;}
        let matches=0;
        candidates.forEach(row=>{
            const match=!selected.has(row.dataset.studentId)&&terms.every(term=>normalize(row.dataset.search).includes(term));
            row.hidden=!match||++matches>8;
            row.querySelector('button').disabled=body.rows.length>=20||row.dataset.blocked==='true';
        });
        document.querySelector('#family-search-status').textContent=body.rows.length>=20?'U arrit kufiri prej 20 lojtarësh.':matches===0?'Nuk ka lojtarë të tjerë për këtë kërkim.':matches>8?`Shfaqen 8 nga ${matches} lojtarë. Shkruani emrin për të ngushtuar kërkimin.`:'';
        results.hidden=matches===0;
    }
    let pricing=JSON.parse(document.querySelector('#family-pricing').textContent), request;
    function update(){
        let total=0;
        [...body.rows].forEach((row,i)=>{
            const amount=body.rows.length===1&&pricing.singleStandard?pricing.standard:i<pricing.limit?pricing.first:pricing.additional;
            total+=Math.round(amount*100);row.querySelector('.position').textContent=String(i+1);row.querySelector('.price').textContent=Number(amount).toFixed(2)+'';
            row.querySelector('[data-move="up"]').disabled=i===0;row.querySelector('[data-move="down"]').disabled=i===body.rows.length-1;
        });
        document.querySelector('#family-total').textContent=(total/100).toFixed(2);
        document.querySelector('#family-rule').textContent=`${pricing.limit} fëmijët e parë nga ${pricing.first}; çdo fëmijë shtesë ${pricing.additional}. Një fëmijë i vetëm: ${pricing.singleStandard?pricing.standard:pricing.first}.`;
        document.querySelector('#family-new-player').disabled=body.rows.length>=20;
        filter();
    }
    results.addEventListener('click',event=>{
        const button=event.target.closest('[data-add]');if(!button||button.disabled)return;
        const candidate=button.closest('[data-student-id]'), id=candidate.dataset.studentId;
        if(body.rows.length>=20||[...body.querySelectorAll('input[name="StudentIds"]')].some(input=>input.value===id))return;
        document.querySelector('#ConfirmIndividualTransfer').checked=false;
        const row=document.createElement('tr');
        row.innerHTML='<td class="position"></td><td><input type="hidden" name="StudentIds"/><span></span></td><td class="price"></td><td class="text-nowrap"><button type="button" class="btn btn-sm btn-outline-secondary" data-move="up" aria-label="Lëviz lart">↑</button> <button type="button" class="btn btn-sm btn-outline-secondary" data-move="down" aria-label="Lëviz poshtë">↓</button> <button type="button" class="btn btn-sm btn-outline-danger" data-move="remove">Hiq</button></td>';
        row.querySelector('input').value=id;row.querySelector('span').textContent=candidate.dataset.name+' · #'+id;body.append(row);update();search.focus();
    });
    body.addEventListener('click',e=>{const button=e.target.closest('[data-move]');if(!button)return;const row=button.closest('tr');if(button.dataset.move==='remove')row.remove();else if(button.dataset.move==='up'&&row.previousElementSibling)body.insertBefore(row,row.previousElementSibling);else if(button.dataset.move==='down'&&row.nextElementSibling)body.insertBefore(row.nextElementSibling,row);update();});
    search.addEventListener('input',filter);
    search.addEventListener('keydown',event=>{if(event.key==='Enter')event.preventDefault();});
    const dialog=document.querySelector('#family-player-dialog'), frame=document.querySelector('#family-player-frame');
    document.querySelector('#family-new-player').addEventListener('click',()=>{
        if(!frame.getAttribute('src'))frame.src=frame.dataset.url;
        dialog.showModal();
    });
    document.querySelector('#family-player-close').addEventListener('click',()=>dialog.close());
    window.addEventListener('message',event=>{
        if(event.origin!==location.origin||event.source!==frame.contentWindow||event.data?.type!=='family-player-created')return;
        const player=event.data.player;
        if(!Number.isInteger(player?.id)||player.id<=0||typeof player.name!=='string')return;
        const id=String(player.id);
        let candidate=candidates.find(row=>row.dataset.studentId===id);
        if(!candidate){
            candidate=document.createElement('div');candidate.className='family-result';candidate.dataset.studentId=id;candidate.dataset.name=player.name;
            candidate.dataset.search=[player.name,player.parent,player.phone,id].filter(Boolean).join(' ');
            candidate.innerHTML='<div><strong></strong><small></small></div><button type="button" class="btn btn-sm btn-outline-primary">+ Shto</button>';
            candidate.querySelector('strong').textContent=player.name;
            candidate.querySelector('small').textContent=(player.parent||'')+' · #'+id;
            const button=candidate.querySelector('button');button.dataset.add=id;button.setAttribute('aria-label','Shto '+player.name);
            candidates.push(candidate);results.append(candidate);
        }
        candidate.querySelector('button').click();
        search.value='';filter();dialog.close();frame.removeAttribute('src');
        document.querySelector('#family-search-status').textContent=player.name+' u regjistrua dhe u shtua. Ruani familjen për ta përfshirë në paketë.';
    });
    async function refreshPricing(){
        document.querySelector('#ConfirmIndividualTransfer').checked=false;
        request?.abort();request=new AbortController();const signal=request.signal;
        try{const response=await fetch(form.dataset.pricingUrl+'?month='+encodeURIComponent(document.querySelector('#EffectiveMonth').value)+'&planId='+encodeURIComponent(document.querySelector('#FeePlanId').value),{signal,cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();if(signal.aborted)return;pricing=data;update();}
        catch(error){if(error.name!=='AbortError')document.querySelector('#family-rule').textContent='Çmimet nuk u ngarkuan. Rihapni formularin për muajin e zgjedhur.';}
    }
    document.querySelector('#EffectiveMonth').addEventListener('change',refreshPricing);
    document.querySelector('#FeePlanId').addEventListener('change',refreshPricing);update();
})();
