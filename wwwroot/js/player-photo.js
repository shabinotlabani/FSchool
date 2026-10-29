document.querySelectorAll('.player-photo-form').forEach(form=>{
 const status=form.querySelector('.photo-status'),file=form.querySelector('input[type=file]'),submit=form.querySelector('button[type=submit]'),preview=form.querySelector('.photo-preview'),previewImage=preview?.querySelector('img');
 let busy=false,previewUrl='';
 function clearPreview(){if(previewUrl)URL.revokeObjectURL(previewUrl);previewUrl='';if(preview)preview.hidden=true;if(previewImage)previewImage.removeAttribute('src');submit.disabled=true;}
 file.addEventListener('change',()=>{
  clearPreview();status.textContent='';const source=file.files[0];if(!source)return;
  if(source.size>10*1024*1024||!['image/jpeg','image/png','image/webp'].includes(source.type)){status.textContent='Zgjidhni JPG, PNG ose WebP deri në 10 MB.';file.value='';return;}
  previewUrl=URL.createObjectURL(source);if(previewImage)previewImage.src=previewUrl;if(preview)preview.hidden=false;submit.disabled=false;
 });
 async function consent(){
  const dialog=document.createElement('dialog');dialog.className='photo-consent';dialog.setAttribute('aria-labelledby','photo-consent-title');
  const title=document.createElement('h2');title.id='photo-consent-title';title.textContent='Përgjegjësia për foton';
  const text=document.createElement('p');text.textContent='Duke vazhduar, konfirmoj se jam prindi ose kujdestari dhe kam të drejtë ta ngarkoj këtë foto. Për përmbajtjen dhe përshtatshmërinë e fotos përgjigjem unë. Klubi nuk merr përgjegjësi për foto të pasakta ose të ngarkuara pa autorizim.';
  const actions=document.createElement('div'),cancel=document.createElement('button'),accept=document.createElement('button');cancel.type=accept.type='button';cancel.textContent='Anulo';accept.textContent='Pranoj dhe ngarkoj';accept.className='photo-consent-accept';actions.append(cancel,accept);dialog.append(title,text,actions);document.body.append(dialog);
  return await new Promise(resolve=>{const finish=value=>{dialog.close();dialog.remove();resolve(value);};cancel.addEventListener('click',()=>finish(false));accept.addEventListener('click',()=>finish(true));dialog.addEventListener('cancel',e=>{e.preventDefault();finish(false);});dialog.showModal();cancel.focus();});
 }
 async function send(remove){
  if(busy)return;
  busy=true;form.querySelectorAll('button').forEach(b=>b.disabled=true);status.textContent='Duke ruajtur…';
  try{
   if(!remove&&form.dataset.parentConsent==='true'&&!await consent()){status.textContent='Ngarkimi u anulua.';return;}
   const data=new FormData(form);data.delete('photo');
   if(!remove&&form.dataset.parentConsent==='true')data.set('photoConsent','true');
   if(remove)data.set('remove','true');
   else{
    const source=file.files[0];if(!source)throw Error('Zgjidhni foton.');
    if(source.size>10*1024*1024||!['image/jpeg','image/png','image/webp'].includes(source.type))throw Error('Zgjidhni JPG, PNG ose WebP deri në 10 MB.');
    const bitmap=await createImageBitmap(source);
    const scale=Math.min(1,600/bitmap.height,480/bitmap.width),canvas=document.createElement('canvas');canvas.width=Math.max(1,Math.round(bitmap.width*scale));canvas.height=Math.max(1,Math.round(bitmap.height*scale));
    const ctx=canvas.getContext('2d');ctx.fillStyle='#edf1f5';ctx.fillRect(0,0,canvas.width,canvas.height);ctx.drawImage(bitmap,0,0,canvas.width,canvas.height);bitmap.close();
    const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/webp',.78));
    if(!blob||blob.type!=='image/webp'||blob.size>204800)throw Error('Fotoja nuk u optimizua. Provoni një foto tjetër.');
    data.set('photo',blob,'player.webp');
   }
   const response=await fetch(form.action,{method:'POST',body:data,credentials:'same-origin'});
   const result=await response.json().catch(()=>({}));if(!response.ok)throw Error(result.message||'Fotoja nuk u ruajt. Rihapni portalin dhe provoni përsëri.');
   const url=new URL(location.href);if(url.pathname.includes('/p/'))url.searchParams.set('continueVisit','true');location.replace(url.href);
  }catch(error){status.textContent=error.message;}finally{busy=false;form.querySelectorAll('button').forEach(b=>b.disabled=false);submit.disabled=!file.files.length;}
 }
 form.addEventListener('submit',e=>{e.preventDefault();send(false);});form.querySelector('[data-remove-photo]')?.addEventListener('click',()=>send(true));
});
