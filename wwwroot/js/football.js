(() => {
 const body=document.querySelector('#football-events'); if(!body)return;
 const form=document.querySelector('#match-form');
 function refresh(){body.querySelectorAll('[data-football-event]').forEach((row,i)=>{
   row.querySelectorAll('[data-field]').forEach(el=>{el.name=`Events[${i}].${el.dataset.field}`;el.removeAttribute('id');});
   row.querySelectorAll('input[name="__Invariant"]').forEach(el=>el.remove());
   const kind=row.querySelector('[data-field="Kind"]').value;
   const opponent=kind==='OpponentGoal'||kind==='OpponentOwnGoal';
   for(const field of ['PlayerId','AssistId']){const el=row.querySelector(`[data-field="${field}"]`);el.disabled=field==='PlayerId'?opponent:kind!=='Goal';if(el.disabled)el.value='';}
   const directRed=kind==='Red';row.querySelector('[data-decision]').hidden=!directRed;
   if(!directRed)for(const field of ['SuspensionGames','Decision'])row.querySelector(`[data-field="${field}"]`).value='';
 });}
 function add(kind){body.append(document.querySelector('#football-event-template').content.cloneNode(true));const row=body.lastElementChild;row.querySelector('[data-field="Kind"]').value=kind;refresh();if(kind==='Red'){body.closest('details').open=true;row.querySelector('[data-decision]').open=true;}return row;}
 window.matchEvents={add};
 body.addEventListener('change',e=>{refresh();if(e.target.matches('[data-field="Kind"]')&&e.target.value==='Red'){body.closest('details').open=true;e.target.closest('[data-football-event]').querySelector('[data-decision]').open=true;}});
 body.addEventListener('click',e=>{const button=e.target.closest('[data-remove-event]');if(button){button.closest('tr').remove();refresh();form.dispatchEvent(new Event('change',{bubbles:true}));}});
 refresh();
 if([...body.querySelectorAll('[data-field="Kind"]')].some(x=>x.value==='Red'))body.closest('details').open=true;
})();
