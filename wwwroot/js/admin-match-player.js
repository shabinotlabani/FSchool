(() => {
 const dialog=document.getElementById('admin-player-dialog');if(!dialog)return;
 const content=document.getElementById('admin-player-content');let request=null,trigger=null;
 document.getElementById('admin-player-close').onclick=()=>dialog.close();
 dialog.addEventListener('close',()=>{request?.abort();trigger?.focus();});
 document.querySelectorAll('a[data-report-player]').forEach(link=>link.addEventListener('click',async event=>{
  if(event.ctrlKey||event.metaKey||event.shiftKey||event.altKey)return;
  event.preventDefault();request?.abort();const current=new AbortController();request=current;trigger=link;
  content.replaceChildren();const loading=document.createElement('h2');loading.id='admin-player-title';loading.textContent='Duke ngarkuar…';content.append(loading);dialog.showModal();
  try{
   const response=await fetch(link.href,{signal:current.signal,credentials:'same-origin',headers:{'X-Requested-With':'XMLHttpRequest'}});
   if(!response.ok||response.redirected)throw new Error('Unavailable');
   const html=await response.text();if(current!==request||!dialog.open)return;
   const parsed=new DOMParser().parseFromString(html,'text/html');
   if(!parsed.getElementById('admin-player-title'))throw new Error('Unavailable');
   content.replaceChildren(...parsed.body.childNodes);
  }catch(error){if(error.name==='AbortError')return;loading.textContent='Të dhënat nuk u hapën. Mbylle dhe provo përsëri.';content.replaceChildren(loading);}
 }));
})();
