(() => {
 const pitch=document.querySelector('#pitch-players');if(!pitch)return;
 const form=document.querySelector('#match-form'), rows=[...form.querySelectorAll('[data-match-player]')], dialog=document.querySelector('#pitch-dialog');
 const preparing=form.dataset.stage==='Draft',locked=form.dataset.stage==='Live';
 dialog.classList.toggle('lineup-phase',preparing);dialog.classList.toggle('events-phase',!preparing);
 const el=id=>document.getElementById(id), role=row=>row.querySelector('.match-role').value, slot=row=>row.querySelector('.pitch-slot');
 const duration=()=>Number(form.querySelector('[name=Duration]').value), capacity=()=>Math.max(1,Math.min(30,Number(form.querySelector('[name=PlayersOnCourt]').value)||11));
 if(locked){const hidden=document.createElement('input');hidden.type='hidden';hidden.name='Formation';hidden.value=el('Formation').value;form.append(hidden);el('Formation').disabled=true;form.querySelector('[name=PlayersOnCourt]').readOnly=true;}
 if(el('finish-match'))el('finish-match').onclick=()=>el('finish-dialog').showModal();
 if(el('finish-cancel'))el('finish-cancel').onclick=()=>el('finish-dialog').close();
 if(el('opponent-goal'))el('opponent-goal').onclick=()=>{const score=form.querySelector('[name=ScoreAgainst]');score.value=(Number(score.value)||0)+1;changed();};
 let selected=null,targetSlot=null;
 let lastMinute='';
 const editor=document.querySelector('.formation-editor');
 document.querySelectorAll('[data-pitch-tab]').forEach(button=>button.onclick=()=>{
  editor.classList.toggle('show-bench',button.dataset.pitchTab==='bench');
  document.querySelectorAll('[data-pitch-tab]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
 });
 function markUnsaved(){el('pitch-save-state').textContent='Pa ruajtur';el('pitch-save-state').classList.add('is-dirty');}
 form.addEventListener('change',e=>{if(e.target.name)markUnsaved();});
 form.addEventListener('input',e=>{if(e.target===form||e.target.name)markUnsaved();});
 el('pitch-minute').addEventListener('input',()=>{lastMinute=el('pitch-minute').value;});
 for(const [id,step] of [['pitch-minute-down',-1],['pitch-minute-up',1]])el(id).onclick=()=>{
  const value=Math.max(1,Math.min(duration(),(Number(el('pitch-minute').value)||0)+step));el('pitch-minute').value=value;lastMinute=String(value);
 };
 const wide=window.matchMedia('(min-width:1100px)');
 const panelHost=el('pitch-panel-host');panelHost.append(dialog);
 const initials=row=>row.dataset.name.trim().split(/\s+/).filter(Boolean).map(n=>n[0]).filter((_,i,a)=>i===0||i===a.length-1).join('').toLocaleUpperCase();
 function position(row){if(role(row)!=='Starter'||slot(row).value==='')return labels[role(row)];let place=Number(slot(row).value),band=0;for(const count of layout()){if(place<count)break;place-=count;band++;}return band===0?'GK':band===1?'DF':band===layout().length-1?'FW':'MF';}
 const iconPaths={minutes:'<circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 2"/>',goals:'<circle cx="12" cy="12" r="9"/><path d="m12 7 5 4-2 6H9l-2-6zM12 3v4m9 5-4-1M6 20l3-3m9 3-3-3M3 12l4-1"/>',assists:'<path d="m4 17 9-9m-6 0h6v6M4 21h16"/>'};
 function metric(kind,value,label){const node=document.createElement('span');node.className=`pitch-metric ${kind}`;node.title=label;node.setAttribute('aria-label',`${label}: ${value}`);
  if(iconPaths[kind]){const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');svg.setAttribute('viewBox','0 0 24 24');svg.setAttribute('aria-hidden','true');svg.innerHTML=iconPaths[kind];node.append(svg);}else{const block=document.createElement('i');block.className=kind==='yellow'?'pitch-yellow':'pitch-red';node.append(block);}
  node.append(document.createTextNode(String(value)+(kind==='minutes'?' min':'')));return node;
 }
 function metrics(row){const values=stats(row).match(/\d+/g)||['0','0','0','0','0'];const group=document.createElement('span');group.className='pitch-card-metrics';['minutes','goals','assists','yellow','red'].forEach((key,i)=>group.append(metric(key,values[i],['Minuta','Gola','Asiste','Të verdhë','Të kuq'][i])));return group;}
 el('lineup-details').open=false;
 function intervals(row){return [...row.querySelectorAll('.match-stint')].map(node=>{const inputs=node.querySelectorAll('input[type=number]');return {node,inputs,from:inputs[0].value===''?null:Number(inputs[0].value),to:inputs[1].value===''?null:Number(inputs[1].value)};});}
 function active(row,m){return intervals(row).some(s=>s.from!==null&&s.to!==null&&s.from<m&&s.to>=m);}
 function stats(row){const id=row.dataset.student;let g=0,a=0,v=0,k=0;
  form.querySelectorAll('[data-football-event]').forEach(e=>{const kind=e.querySelector('[data-field=Kind]').value;if(e.querySelector('[data-field=PlayerId]').value===id){if(['Goal','PenaltyGoal'].includes(kind))g++;if(['Yellow','SecondYellow'].includes(kind))v++;if(['Red','SecondYellow'].includes(kind))k++;}if(kind==='Goal'&&e.querySelector('[data-field=AssistId]').value===id)a++;});
  return `${row.querySelector('.match-minutes').textContent} min · G ${g} · A ${a} · V ${v} · K ${k}`;
 }
 const labels={Starter:'Titullar',Substitute:'Nga stoli',Unused:'Rezervë',NotSelected:'Jashtë listës'};
 function card(row){const button=document.createElement('button');button.type='button';button.className='pitch-player-card';button.dataset.playerIndex=row.dataset.index;
  button.classList.toggle('is-selected',selected===row);button.setAttribute('aria-pressed',String(selected===row));
  const avatar=document.createElement('span');avatar.className='pitch-avatar';avatar.textContent=initials(row);avatar.setAttribute('aria-hidden','true');
  const identity=document.createElement('span');identity.className='pitch-card-identity';const name=document.createElement('strong');name.textContent=row.dataset.name;
  const status=document.createElement('span');status.textContent=position(row);status.className='pitch-card-role';identity.append(name,status);button.append(avatar,identity,metrics(row));button.classList.toggle('is-goalkeeper',position(row)==='GK');
  button.setAttribute('aria-label',`${row.dataset.name}, ${labels[role(row)]}, ${stats(row)}`);button.addEventListener('click',()=>open(row));return button;
 }
 function layout(){const cap=capacity();if(cap===11&&el('Formation').value!=='Auto')return [1,...el('Formation').value.split('-').map(Number)];
  if(cap===1)return [1];const remaining=cap-1,lines=Math.ceil(remaining/4),base=Math.floor(remaining/lines),extra=remaining%lines;return [1,...Array.from({length:lines},(_,i)=>base+(i<extra?1:0))];}
 function render(){const starters=rows.filter(r=>role(r)==='Starter'),used=new Set();
  starters.forEach(r=>{const value=Number(slot(r).value);if(slot(r).value===''||value>=capacity()||used.has(value))slot(r).value='';else used.add(value);});
  starters.filter(r=>slot(r).value==='').forEach(r=>{for(let i=0;i<capacity();i++)if(!used.has(i)){slot(r).value=i;used.add(i);break;}});
  pitch.replaceChildren();let index=0;
  layout().forEach((count,line)=>{const band=document.createElement('div');band.className='pitch-line';band.style.setProperty('--players',count);
   for(let i=0;i<count;i++,index++){const place=index,row=starters.find(r=>slot(r).value!==''&&Number(slot(r).value)===place);
    if(row)band.append(card(row));else {const button=document.createElement('button');button.type='button';button.className='pitch-empty-slot';button.textContent='+';button.setAttribute('aria-label',line===0?'Zgjidh portierin':`Zgjidh lojtarin për vendin ${place+1}`);button.addEventListener('click',()=>open(null,place));band.append(button);}}
   pitch.append(band);
  });
  const bench=rows.filter(r=>['Unused','Substitute'].includes(role(r))),available=rows.filter(r=>role(r)==='NotSelected'||role(r)==='Starter'&&slot(r).value==='');
  el('pitch-bench').replaceChildren(...bench.map(card));el('pitch-available').replaceChildren(...available.map(card));el('pitch-bench-count').textContent=bench.length;el('pitch-available-count').textContent=available.length;
  if(dialog.open)refreshDialog();
 }
 function refreshDialog(){el('pitch-player-name').textContent=selected?selected.dataset.name:'Vendos lojtarin';el('pitch-player-status').textContent=selected?labels[role(selected)]:'Formacioni fillestar';el('pitch-player-stats').textContent=selected?stats(selected):'';el('pitch-care').textContent=selected?.querySelector('.match-care')?.textContent??'';
  const rating=selected?.querySelector('.match-rating').value||'';
  dialog.querySelector('.pitch-assessment').hidden=preparing||!selected||role(selected)==='NotSelected';
  dialog.querySelectorAll('[data-rating]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.rating!==''&&Number(b.dataset.rating)<=Number(rating))));
  el('pitch-note').value=selected?.querySelector('.match-note').value||'';
  el('pitch-selected-avatar').textContent=selected?initials(selected):'+';
  el('pitch-pick-label').hidden=!!selected;dialog.querySelectorAll('[data-pitch-role],#pitch-enter,#pitch-leave,#pitch-swap-apply,[data-pitch-event],#pitch-details').forEach(b=>b.disabled=!selected);
  dialog.querySelectorAll('[data-pitch-role]').forEach(b=>b.setAttribute('aria-pressed',String(!!selected&&role(selected)===b.dataset.pitchRole)));
  [...el('pitch-swap-player').options].forEach(o=>o.disabled=!!selected&&o.value===selected.dataset.index);
  if(!preparing){dialog.querySelectorAll('[data-pitch-role]').forEach(b=>b.hidden=b.dataset.pitchRole==='Starter'||!!selected&&['Starter','Substitute'].includes(role(selected)));}
  el('pitch-swap-label').textContent=selected&&role(selected)==='Unused'?'Kush del nga loja?':'Me kë zëvendësohet?';
 }
 dialog.querySelectorAll('[data-rating]').forEach(b=>b.onclick=()=>{if(!selected)return;selected.querySelector('.match-rating').value=b.dataset.rating;changed();});
 el('pitch-note').oninput=()=>{if(!selected)return;selected.querySelector('.match-note').value=el('pitch-note').value;markUnsaved();};
 function open(row,place=null){selected=row;targetSlot=place;el('pitch-message').textContent='';el('pitch-minute').value=lastMinute;el('pitch-swap-player').value='';el('pitch-pick').value='';dialog.querySelector('.pitch-swap').open=false;render();refreshDialog();if(!dialog.open){if(wide.matches)dialog.show();else dialog.showModal();}panelHost.querySelector('.pitch-selection-empty').hidden=true;}
 dialog.addEventListener('close',()=>{panelHost.querySelector('.pitch-selection-empty').hidden=dialog.open;});
 wide.addEventListener('change',()=>{if(dialog.open){dialog.close();if(wide.matches)dialog.show();else dialog.showModal();panelHost.querySelector('.pitch-selection-empty').hidden=true;}});
 function change(row,newRole){const control=row.querySelector('.match-role');control.value=newRole;control.dispatchEvent(new Event('change',{bubbles:true}));markUnsaved();}
 function changed(){form.dispatchEvent(new Event('input',{bubbles:true}));render();}
 function message(value){el('pitch-message').textContent=value;}
 function minute(allowEnd=false){const text=el('pitch-minute').value,m=Number(text);if(text===''||!Number.isInteger(m)||m<=0||m>duration()||(!allowEnd&&m===duration())){message('Shkruaj minutën brenda kohëzgjatjes së ndeshjes.');return null;}return m;}
 el('pitch-auto').onclick=()=>{
  let added=0;
  const candidates=rows.filter(r=>['NotSelected','Unused'].includes(role(r))&&!r.querySelector('.match-care')&&intervals(r).length===0);
  for(const row of candidates){if(rows.filter(r=>role(r)==='Starter').length>=capacity())break;change(row,'Starter');added++;}
  rows.filter(r=>role(r)==='NotSelected'&&!r.querySelector('.match-care')&&intervals(r).length===0).forEach(r=>change(r,'Unused'));
  el('pitch-auto-message').textContent=added?`${added} titullarë u shtuan. Mund t’i ndryshosh nga kartat.`:'Formacioni nuk ndryshoi: vendet janë plot ose nuk ka lojtarë të tjerë të disponueshëm.';
  render();
 };
 el('pitch-close').onclick=()=>dialog.close();
 el('pitch-pick').onchange=()=>{if(el('pitch-pick').value==='')return;selected=rows[Number(el('pitch-pick').value)];slot(selected).value=targetSlot;change(selected,'Starter');message('U vendos në formacion.');};
 dialog.querySelectorAll('[data-pitch-role]').forEach(b=>b.onclick=()=>{if(!selected)return;if(b.dataset.pitchRole==='Starter'&&role(selected)!=='Starter'&&rows.filter(r=>role(r)==='Starter').length>=capacity()){message('Formacioni është plot. Liro një vend ose përdor ndërrimin.');return;}change(selected,b.dataset.pitchRole);message('Roli u ndryshua. Ruaj ndeshjen.');});
 el('pitch-enter').onclick=()=>{if(!selected)return;const m=minute();if(m===null)return;if(intervals(selected).some(s=>s.from===null||s.to===null||s.to>m)){message('Lojtari ka minuta të regjistruara pas kësaj minute. Përdor rregullimin e minutave.');return;}
  if(rows.filter(r=>active(r,m)).length>=capacity()){message('Fusha është plot. Përdor ndërrimin.');return;}
  if(role(selected)!=='Starter')change(selected,'Substitute');
  let stint=intervals(selected).find(s=>s.from===null&&s.to===null);if(!stint){selected.querySelector('[data-add-stint]').click();stint=intervals(selected).at(-1);}stint.inputs[0].value=m;stint.inputs[1].value=duration();changed();message('Hyrja në lojë u regjistrua.');
 };
 function leave(m){const stint=intervals(selected).find(s=>s.from!==null&&s.to!==null&&s.from<m&&s.to>=m);if(!stint)return false;stint.inputs[1].value=m;changed();return true;}
 el('pitch-leave').onclick=()=>{if(!selected)return;const m=minute();if(m!==null)message(leave(m)?'Dalja u regjistrua.':'Lojtari nuk është në lojë në këtë minutë.');};
 el('pitch-swap-apply').onclick=()=>{if(!selected)return;const m=minute(),other=rows[Number(el('pitch-swap-player').value)];if(m===null)return;if(el('pitch-swap-player').value===''||!other||other===selected){message('Zgjidh lojtarin tjetër.');return;}
  const outgoing=active(selected,m)?selected:other,incoming=outgoing===selected?other:selected;
  el('sub-out').value=outgoing.dataset.index;el('sub-in').value=incoming.dataset.index;el('sub-minute').value=m;el('apply-substitution').click();render();markUnsaved();message(el('sub-message').textContent);
 };
 dialog.querySelectorAll('[data-pitch-event]').forEach(b=>b.onclick=()=>{if(!selected)return;const m=minute(true);if(m===null)return;if(role(selected)==='NotSelected'){message('Vendose lojtarin në listën e ndeshjes.');return;}
  let kind=b.dataset.pitchEvent;if(kind==='Goal'&&!active(selected,m)){message('Golashënuesi duhet të jetë në lojë.');return;}
  const existing=[...form.querySelectorAll('[data-football-event]')].filter(e=>e.querySelector('[data-field=PlayerId]').value===selected.dataset.student);
  if(existing.some(e=>['Red','SecondYellow'].includes(e.querySelector('[data-field=Kind]').value))){message('Lojtari ka një përjashtim të regjistruar.');return;}
  if(kind==='Yellow'&&existing.some(e=>e.querySelector('[data-field=Kind]').value==='Yellow'))kind='SecondYellow';
  el(kind==='Goal'?'add-goal':'add-card').click();const row=el('football-events').lastElementChild;
  row.querySelector('[data-field=Kind]').value=kind;row.querySelector('[data-field=PlayerId]').value=selected.dataset.student;row.querySelector('[data-field=Minute]').value=m;row.querySelector('[data-field=Kind]').dispatchEvent(new Event('change',{bubbles:true}));
  if(kind==='Goal'){const score=form.querySelector('[name=ScoreFor]');score.value=(Number(score.value)||0)+1;}
  if(['Red','SecondYellow'].includes(kind)){leave(m);intervals(selected).filter(s=>s.from!==null&&s.from>=m).forEach(s=>s.node.querySelector('[data-remove-stint]').click());}
  changed();message(kind==='SecondYellow'?'I dyti i verdhë: përjashtim. Dalja u përditësua.':'Ngjarja u shtua. Ruaj ndeshjen.');
 });
 el('pitch-details').onclick=()=>{dialog.close();el('lineup-details').open=true;selected?.scrollIntoView({block:'center',behavior:'smooth'});};
 form.addEventListener('match:updated',render);
 form.addEventListener('change',render);
 form.addEventListener('click',e=>{if(e.target.closest('[data-remove-event]'))render();});
 form.addEventListener('invalid',e=>{if(e.target.closest('#lineup-details'))el('lineup-details').open=true;if(e.target.closest('.match-metadata'))e.target.closest('.match-metadata').open=true;},true);
 if(form.querySelector('.validation-summary-errors'))markUnsaved();
 render();
})();
