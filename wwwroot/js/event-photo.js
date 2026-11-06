document.querySelector('#event-edit')?.addEventListener('submit', async function(event) {
 const file=this.querySelector('[data-event-photo]');
 if(!file?.files.length||this.dataset.photoReady==='true')return;
 event.preventDefault();const status=this.querySelector('.photo-status'),button=this.querySelector('button[type=submit]');
 button.disabled=true;
 try{
  const source=file.files[0];
  if(source.size>10*1024*1024||!['image/jpeg','image/png','image/webp'].includes(source.type))throw Error('Zgjidhni JPG, PNG ose WebP deri në 10 MB.');
  const bitmap=await createImageBitmap(source),scale=Math.min(1,480/bitmap.width,600/bitmap.height),canvas=document.createElement('canvas');
  canvas.width=Math.max(1,Math.round(bitmap.width*scale));canvas.height=Math.max(1,Math.round(bitmap.height*scale));
  const ctx=canvas.getContext('2d');ctx.fillStyle='#fff';ctx.fillRect(0,0,canvas.width,canvas.height);ctx.drawImage(bitmap,0,0,canvas.width,canvas.height);bitmap.close();
  const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/webp',.78));
  if(!blob||blob.type!=='image/webp'||blob.size>204800)throw Error('Fotoja nuk u përpunua. Provoni një tjetër.');
  const transfer=new DataTransfer();transfer.items.add(new File([blob],'event.webp',{type:'image/webp'}));file.files=transfer.files;
  this.dataset.photoReady='true';this.requestSubmit();
 }catch(error){status.textContent=error.message;}finally{button.disabled=false;}
});
