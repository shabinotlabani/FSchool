window.initParentEvents = () => {
 window.cleanupParentEvents?.();
 const content=document.querySelector('[data-parent-events-url]');if(!content)return;
 let busy=false,loaded=false,disposed=false;
 const message='Eventet nuk u ngarkuan. Provoni përsëri.';
 async function load(url,options={}){
  if(busy)return;busy=true;content.setAttribute('aria-busy','true');
  const controls=[...content.querySelectorAll('button')];controls.forEach(button=>button.disabled=true);
  try{
   const response=await fetch(url,{credentials:'same-origin',...options});
   if(!response.ok)throw Error(message);
   const html=await response.text();if(disposed)return;const parsed=new DOMParser().parseFromString(html,'text/html');
   const panel=parsed.querySelector('.parent-event-content');if(!panel)throw Error(message);
   content.replaceChildren(panel);loaded=true;
   const count=panel.dataset.eventPending;
   const caption=document.querySelector('[aria-controls="panel-events"] small');
   if(caption)caption.textContent=Number(count)>0?`${count} kërkesa në pritje`:'Aktivitetet dhe autorizimet';
   const notice=panel.querySelector('[role="status"], [role="alert"]');
   if(notice){notice.setAttribute('tabindex','-1');notice.focus();}
  }catch(error){
   let notice=content.querySelector('[data-event-load-error]');
   if(!notice){notice=document.createElement('p');notice.className='event-error';notice.dataset.eventLoadError='';notice.setAttribute('role','alert');content.prepend(notice);}
   notice.textContent=error.message;
   if(!loaded){const retry=document.createElement('button');retry.type='button';retry.className='parent-event-retry';retry.textContent='Provo përsëri';retry.onclick=()=>load(content.dataset.parentEventsUrl);notice.append(document.createElement('br'),retry);}
  }finally{busy=false;content.removeAttribute('aria-busy');controls.filter(button=>button.isConnected).forEach(button=>button.disabled=false);}
 }
 const onOpen=event=>{if(event.detail.id==='events')load(content.dataset.parentEventsUrl);};
 document.addEventListener('portal-panel-open',onOpen);
 window.cleanupParentEvents=()=>{disposed=true;document.removeEventListener('portal-panel-open',onOpen);};
 content.addEventListener('click',event=>{
  const link=event.target.closest('a');if(!link)return;
  const url=new URL(link.href);const base=new URL(content.dataset.parentEventsUrl,location.href);
  if(url.origin===base.origin&&url.pathname===base.pathname){event.preventDefault();url.searchParams.set('panel','true');load(url.href);}
 });
 content.addEventListener('submit',event=>{
  const form=event.target;if(!(form instanceof HTMLFormElement))return;
  event.preventDefault();if(busy)return;
  const data=new FormData(form);data.set('panel','true');
  if(event.submitter?.name)data.set(event.submitter.name,event.submitter.value);
  // Reopen the event that was just answered so its result stays visible.
  const eventId=form.closest('.parent-event-item')?.id;
  load(form.action,{method:'POST',body:data}).then(()=>{
   if(eventId){const item=content.querySelector('#'+CSS.escape(eventId));if(item)item.open=true;}
  });
 });
};
window.initParentEvents();
