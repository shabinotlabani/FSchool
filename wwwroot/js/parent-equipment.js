window.initParentEquipment=()=>{
 window.cleanupParentEquipment?.();
 const popup=document.querySelector('[data-parent-equipment-url]');
 const content=popup||document.querySelector('.parent-equipment-content');if(!content)return;
 let busy=false,disposed=false;
 const money=new Intl.NumberFormat('sq-AL',{minimumFractionDigits:2,maximumFractionDigits:2});
 function update(form,clamp=false){
  const quantity=form.querySelector('[name=Quantity]');if(!quantity)return;
  const max=Number(form.querySelector('[name=VariantId]:checked')?.dataset.available)||100;
  quantity.max=max;
  if(clamp)quantity.value=Math.min(max,Math.max(1,Math.floor(Number(quantity.value)||1)));
  const count=Number(quantity.value),price=Number(form.dataset.unitPrice);
  const output=form.querySelector('output');if(output)output.textContent=Number.isFinite(count)&&Number.isFinite(price)?money.format(count*price):'—';
  form.querySelectorAll('[data-quantity-step]').forEach(button=>button.disabled=busy||(Number(button.dataset.quantityStep)<0?count<=1:count>=max));
 }
 const refresh=()=>content.querySelectorAll('.equipment-order-form').forEach(form=>update(form));
 content.addEventListener('change',event=>{const form=event.target.closest('.equipment-order-form');if(form)update(form,event.target.name==='VariantId');});
 content.addEventListener('input',event=>{const form=event.target.closest('.equipment-order-form');if(form)update(form);});
 content.addEventListener('click',event=>{
  const button=event.target.closest('[data-quantity-step]');if(!button||busy)return;
  const form=button.closest('form'),quantity=form.querySelector('[name=Quantity]');
  quantity.value=(Number(quantity.value)||1)+Number(button.dataset.quantityStep);update(form,true);
 });
 refresh();
 async function load(url,options={}){
  if(busy)return;busy=true;content.setAttribute('aria-busy','true');
  content.querySelectorAll('button').forEach(b=>b.disabled=true);
  try{
   const response=await fetch(url,{credentials:'same-origin',...options});
   if(!response.ok)throw Error('Rekuizitat nuk u ngarkuan. Provoni përsëri.');
   const html=await response.text();if(disposed)return;
   const panel=new DOMParser().parseFromString(html,'text/html').querySelector('.parent-equipment-content');
   if(!panel)throw Error('Rihapni portalin dhe provoni përsëri.');
   content.replaceChildren(panel);refresh();
   const notice=panel.querySelector('[role=status],[role=alert]');if(notice){notice.tabIndex=-1;notice.focus();}
  }catch(error){
   if(disposed)return;
   let notice=content.querySelector('[data-equipment-error]');
   if(!notice){notice=document.createElement('p');notice.dataset.equipmentError='';notice.className='equipment-error';notice.setAttribute('role','alert');content.prepend(notice);}
   notice.textContent=error.message;
  }finally{busy=false;if(!disposed){content.removeAttribute('aria-busy');content.querySelectorAll('button').forEach(b=>b.disabled=false);refresh();}}
 }
 const open=event=>{if(popup&&event.detail.id==='equipment')load(content.dataset.parentEquipmentUrl);};
 document.addEventListener('portal-panel-open',open);
 window.cleanupParentEquipment=()=>{disposed=true;document.removeEventListener('portal-panel-open',open);};
 content.addEventListener('submit',event=>{
  const form=event.target;if(!(form instanceof HTMLFormElement))return;
  if(!popup)return;
  event.preventDefault();if(busy)return;
  const data=new FormData(form);data.set('panel','true');load(form.action,{method:'POST',body:data});
 });
};
window.initParentEquipment();
