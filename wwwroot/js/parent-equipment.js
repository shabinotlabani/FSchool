window.initParentEquipment=()=>{
 window.cleanupParentEquipment?.();
 const content=document.querySelector('[data-parent-equipment-url]');if(!content)return;
 let busy=false,disposed=false;
 async function load(url,options={}){
  if(busy)return;busy=true;content.setAttribute('aria-busy','true');
  content.querySelectorAll('button').forEach(b=>b.disabled=true);
  try{
   const response=await fetch(url,{credentials:'same-origin',...options});
   if(!response.ok)throw Error('Rekuizitat nuk u ngarkuan. Provoni përsëri.');
   const html=await response.text();if(disposed)return;
   const panel=new DOMParser().parseFromString(html,'text/html').querySelector('.parent-equipment-content');
   if(!panel)throw Error('Rihapni portalin dhe provoni përsëri.');
   content.replaceChildren(panel);
   const notice=panel.querySelector('[role=status],[role=alert]');if(notice){notice.tabIndex=-1;notice.focus();}
  }catch(error){
   if(disposed)return;
   let notice=content.querySelector('[data-equipment-error]');
   if(!notice){notice=document.createElement('p');notice.dataset.equipmentError='';notice.className='equipment-error';notice.setAttribute('role','alert');content.prepend(notice);}
   notice.textContent=error.message;
  }finally{busy=false;if(!disposed){content.removeAttribute('aria-busy');content.querySelectorAll('button').forEach(b=>b.disabled=false);}}
 }
 const open=event=>{if(event.detail.id==='equipment')load(content.dataset.parentEquipmentUrl);};
 document.addEventListener('portal-panel-open',open);
 window.cleanupParentEquipment=()=>{disposed=true;document.removeEventListener('portal-panel-open',open);};
 content.addEventListener('submit',event=>{
  const form=event.target;if(!(form instanceof HTMLFormElement))return;
  event.preventDefault();if(busy)return;
  const data=new FormData(form);data.set('panel','true');load(form.action,{method:'POST',body:data});
 });
};
window.initParentEquipment();
