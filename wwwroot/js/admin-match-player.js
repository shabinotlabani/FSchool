(() => {
 const dialog=document.getElementById('admin-player-dialog');if(!dialog)return;
 const content=document.getElementById('admin-player-content');let request=null,trigger=null;
 document.getElementById('admin-player-close').onclick=()=>dialog.close();
 dialog.addEventListener('close',()=>{request?.abort();trigger?.focus();});
 async function load(url,filter=false){
  request?.abort();const current=new AbortController();request=current;
  content.replaceChildren();const loading=document.createElement('h2');loading.id='admin-player-title';loading.textContent='Duke ngarkuar…';content.append(loading);if(!dialog.open)dialog.showModal();
  try{
   const response=await fetch(url,{signal:current.signal,credentials:'same-origin',headers:{'X-Requested-With':'XMLHttpRequest'}});
   if(!response.ok||response.redirected)throw new Error('Unavailable');
   const html=await response.text();if(current!==request||!dialog.open)return;
   const parsed=new DOMParser().parseFromString(html,'text/html');
   if(!parsed.getElementById('admin-player-title'))throw new Error('Unavailable');
   content.replaceChildren(...parsed.body.childNodes);
   if(filter)content.querySelector('#player-league')?.focus();
  }catch(error){if(error.name==='AbortError')return;loading.textContent='Të dhënat nuk u hapën. Mbylle dhe provo përsëri.';content.replaceChildren(loading);}
 }
 document.querySelectorAll('a[data-report-player]').forEach(link=>link.addEventListener('click',event=>{
  if(event.ctrlKey||event.metaKey||event.shiftKey||event.altKey)return;
  event.preventDefault();trigger=link;load(link.href);
 }));
 content.addEventListener('submit',event=>{
  if(!event.target.matches('.admin-player-filter'))return;
  event.preventDefault();const url=new URL(event.target.action,location.href);url.search=new URLSearchParams(new FormData(event.target)).toString();load(url.href,true);
 });
})();
