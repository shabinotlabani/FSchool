(()=>{
    const birth=document.querySelector('#DateOfBirth'),area=document.querySelector('#team-selection');
    if(!birth||!area)return;
    const list=document.querySelector('#team-proposals'),status=document.querySelector('#team-status'),none=document.querySelector('#no-team');
    let request,sequence=0;
    function node(tag,text,className){const element=document.createElement(tag);if(text!=null)element.textContent=text;if(className)element.className=className;return element;}
    async function load(selected=''){
        request?.abort();const current=++sequence;
        if(!birth.value||!birth.validity.valid){list.replaceChildren();none.checked=true;status.textContent='Vendosni një datëlindje të vlefshme për propozimin e ekipeve.';return;}
        request=new AbortController();status.textContent='Duke kontrolluar ekipet dhe vendet e lira…';
        try{
            const url=new URL(area.dataset.endpoint,window.location.origin);url.searchParams.set('dateOfBirth',birth.value);
            const response=await fetch(url,{signal:request.signal,headers:{Accept:'application/json'},cache:'no-store'});
            if(!response.ok)throw new Error('Unavailable');
            const result=await response.json();if(current!==sequence)return;
            list.replaceChildren();none.checked=true;
            for(const team of result.teams){
                const label=node('label',null,'team-choice'),radio=node('input');radio.type='radio';radio.name='TrainingTeamId';radio.value=String(team.id);radio.disabled=team.freePlaces===0;
                const content=node('span');content.append(node('strong',team.name),node('small',`${team.minAge}–${team.maxAge} vjeç · ${team.freePlaces===0?'Ekipi është i mbushur':team.freePlaces+' vende të lira'} (${team.enrolled}/${team.capacity})`),node('span',team.sessions.map(s=>`${s.day} ${s.start}–${s.end}`).join('\n'),'team-choice-times'));
                if(team.location)content.append(node('small',team.location));label.append(radio,content);list.append(label);
                if(String(team.id)===String(selected)&&!radio.disabled)radio.checked=true;
            }
            status.textContent=`Mosha: ${result.age} vjeç. `+(result.teams.length?'Zgjidhni ekipin sipas ditëve, orarit dhe vendeve të lira.':'Nuk ka ekip aktiv për këtë moshë. Mund ta caktoni më vonë.');
        }catch(error){if(error.name==='AbortError'||current!==sequence)return;status.textContent='Propozimet nuk u ngarkuan. Provoni ta zgjidhni datëlindjen përsëri ose regjistrojeni pa ekip.';}
    }
    birth.addEventListener('change',()=>{list.replaceChildren();none.checked=true;load();});
    if(birth.value)load(area.dataset.selected);
})();
