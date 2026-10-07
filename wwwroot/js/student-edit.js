(() => {
    const form=document.querySelector('#student-edit'),birth=document.querySelector('#DateOfBirth'),select=document.querySelector('#TrainingTeamId'),status=document.querySelector('#edit-team-status');let pending;
    birth.addEventListener('change',async()=>{
        pending?.abort();pending=new AbortController();const signal=pending.signal,selected=select.value,oldText=select.selectedOptions[0]?.text;
        if(!birth.value)return;
        try{
            const response=await fetch(form.dataset.teamsUrl+'?dateOfBirth='+encodeURIComponent(birth.value)+'&excludingStudentId='+encodeURIComponent(document.querySelector('#Id').value),{signal,cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();if(signal.aborted)return;
            select.replaceChildren(new Option('Pa ekip',''));
            data.teams.forEach(team=>{const option=new Option(`${team.name} · ${team.freePlaces} vende · ${team.sessions.map(s=>`${s.day} ${s.start}–${s.end}`).join('; ')}`,team.id);option.disabled=team.freePlaces===0;select.add(option);});
            if(selected&&![...select.options].some(option=>option.value===selected))select.add(new Option(`${oldText} — nuk përputhet me datëlindjen e re`,selected));
            select.value=selected;status.textContent='Kontrolloni ekipin sipas datëlindjes së re. Kapaciteti dhe mosha verifikohen në ruajtje.';
        }catch(error){if(error.name!=='AbortError')status.textContent='Nuk u ngarkuan ekipet. Kontrolloni datëlindjen dhe provoni përsëri.';}
    });
})();
