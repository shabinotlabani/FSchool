(() => {
 const selector=document.querySelector('.parent-athlete-selector');
 if(!selector||!window.fetch||!window.AbortController)return;
 const choices=[...selector.querySelectorAll('.parent-athlete-choice')];
 if(choices.length<2)return;
 let request=null,sequence=0,pending=null,start=null;
 const reduced=matchMedia('(prefers-reduced-motion: reduce)');
 const status=document.createElement('p');status.className='athlete-switch-status';status.setAttribute('role','status');status.setAttribute('aria-live','polite');selector.after(status);
 const current=()=>document.getElementById('parent-athlete-content');
 const initialStudent=current()?.dataset.studentId;
 const index=()=>choices.findIndex(link=>link.dataset.studentId===current()?.dataset.studentId);
 async function change(link,push=true){
  const old=current();if(!old||!link)return;
  const target=link.dataset.studentId;
  if(target===pending||target===old.dataset.studentId&&!request)return;
  const previous=index(),next=choices.indexOf(link),direction=next>=previous?1:-1;
  request?.abort();const controller=new AbortController();request=controller;pending=target;
  const ticket=++sequence;
  old.setAttribute('aria-busy','true');old.inert=true;selector.classList.add('is-switching');status.classList.remove('error');status.textContent='';
  const address=new URL(link.href);address.searchParams.delete('finance');address.hash='';
  const endpoint=new URL(address);endpoint.searchParams.set('profile','true');
  try{
   const response=await fetch(endpoint,{credentials:'same-origin',signal:controller.signal});
   if(!response.ok)throw Error();
   const html=await response.text();
   const parsed=new DOMParser().parseFromString(html,'text/html'),profile=parsed.getElementById('parent-athlete-content');
   if(!profile||profile.dataset.studentId!==target)throw Error();
   const portrait=profile.querySelector('.player-portrait');
   if(portrait){const image=new Image();image.src=new URL(portrait.getAttribute('src'),location.href);await Promise.race([image.decode().catch(()=>{}),new Promise(resolve=>setTimeout(resolve,250))]);}
   if(ticket!==sequence||controller.signal.aborted)return;
   window.cleanupParentEvents?.();window.cleanupParentEquipment?.();window.cleanupParentPanels?.();
   profile.style.minHeight=old.offsetHeight+'px';old.replaceWith(profile);
   choices.forEach(choice=>{
    const active=choice===link;choice.classList.toggle('selected',active);
    if(active)choice.setAttribute('aria-current','page');else choice.removeAttribute('aria-current');
    choice.querySelector('small')?.remove();
    const label=document.createElement('small');label.textContent=active?'Profili aktiv':'Profili';choice.append(label);
   });
   if(push)history.pushState({parentStudent:target},'',address);
   document.body.dataset.parentReopenUrl=profile.dataset.reopenUrl;
   window.initPlayerProgress?.();window.initPlayerPhotos?.();window.initParentEvents?.();window.initParentEquipment?.();window.initParentPanels?.();
   const animation=!reduced.matches&&profile.animate?profile.animate([{opacity:.45,transform:`translateX(${direction*24}px)`},{opacity:1,transform:'translateX(0)'}],{duration:200,easing:'cubic-bezier(.2,.75,.25,1)'}):null;
   const release=()=>{if(profile.isConnected)profile.style.minHeight='';};
   if(animation)animation.finished.then(release,release);else release();
   status.textContent='Profili i '+profile.dataset.studentName;
  }catch(error){
   if(error.name==='AbortError'||ticket!==sequence)return;
   status.classList.add('error');
   status.textContent='Kalimi nuk u krye. Prekni përsëri fëmijën për ta provuar.';
  }finally{
   if(ticket===sequence){request=null;pending=null;const active=current();if(active){active.removeAttribute('aria-busy');active.inert=false;}selector.classList.remove('is-switching');}
  }
 }
 selector.addEventListener('click',event=>{
  const link=event.target.closest('.parent-athlete-choice');
  if(!link||event.ctrlKey||event.metaKey||event.shiftKey||event.altKey||event.button!==0)return;
  event.preventDefault();change(link);
 });
 let tap=null;
 selector.addEventListener('pointerdown',event=>{
  if(event.pointerType==='touch'&&event.isPrimary)tap={id:event.pointerId,x:event.clientX,y:event.clientY};
 });
 selector.addEventListener('pointercancel',()=>{tap=null;});
 selector.addEventListener('pointerup',event=>{
  const point=tap;tap=null;
  if(!point||point.id!==event.pointerId||Math.abs(event.clientX-point.x)>12||Math.abs(event.clientY-point.y)>12)return;
  const link=event.target.closest('.parent-athlete-choice');
  if(link){event.preventDefault();change(link);}
 });
 document.addEventListener('pointerdown',event=>{
  if(event.pointerType!=='touch'||!event.isPrimary||!event.target.closest('#parent-athlete-content')||event.target.closest('a,button,input,textarea,select,summary,.event-scroll,details[open],.progress-chart'))return;
  start={x:event.clientX,y:event.clientY,id:event.pointerId,time:performance.now()};
 });
 document.addEventListener('pointercancel',()=>{start=null;});
 document.addEventListener('pointerup',event=>{
  if(!start||start.id!==event.pointerId)return;
  const dx=event.clientX-start.x,dy=event.clientY-start.y,elapsed=performance.now()-start.time;start=null;
  if(elapsed>900||Math.abs(dx)<60||Math.abs(dx)<Math.abs(dy)*1.5)return;
  change(choices[index()+(dx<0?1:-1)]);
 });
 window.addEventListener('popstate',()=>{
  const selected=new URL(location.href).searchParams.get('studentId')||initialStudent;
  const link=choices.find(choice=>choice.dataset.studentId===selected)||choices[0];
  if(link.dataset.studentId!==current()?.dataset.studentId)change(link,false);
 });
})();
