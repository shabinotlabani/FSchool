(() => {
 const root=document.querySelector('.portal-actions');if(!root||typeof HTMLDialogElement==='undefined')return;
 const panels=new Map();let current=null;
 function open(id,push=true){const item=panels.get(id);if(!item)return;if(current){panels.get(current).dialog.close();panels.get(current).button.setAttribute('aria-expanded','false');}current=id;item.dialog.showModal();item.button.setAttribute('aria-expanded','true');document.body.classList.add('portal-panel-open');if(push)history.pushState({portalPanel:id},'',location.pathname+location.search+'#'+id);}
 function close(){if(!current)return;const item=panels.get(current);item.dialog.close();item.button.setAttribute('aria-expanded','false');item.button.focus();current=null;document.body.classList.remove('portal-panel-open');}
 function dismiss(){if(history.state?.portalPanel)history.back();else{close();history.replaceState(null,'',location.pathname+location.search);}}
 root.querySelectorAll('details[id]').forEach(section=>{
  const id=section.id,summary=section.querySelector(':scope > summary'),content=section.querySelector(':scope > .parent-care-content');if(!summary||!content)return;
  const button=document.createElement('button');button.type='button';button.className='portal-action-button';button.innerHTML=summary.innerHTML;button.setAttribute('aria-haspopup','dialog');button.setAttribute('aria-expanded','false');button.setAttribute('aria-controls','panel-'+id);
  const dialog=document.createElement('dialog');dialog.id='panel-'+id;dialog.className='parent-panel';dialog.setAttribute('aria-labelledby','title-'+id);
  const header=document.createElement('header'),title=document.createElement('h2'),back=document.createElement('button');title.id='title-'+id;title.textContent=summary.querySelector('span')?.textContent||'';back.type='button';back.className='panel-close';back.textContent='×';back.setAttribute('aria-label','Mbyll');back.addEventListener('click',dismiss);header.append(title,back);dialog.append(header,content);document.body.append(dialog);section.replaceWith(button);panels.set(id,{dialog,button});
  button.addEventListener('click',()=>open(id));dialog.addEventListener('cancel',e=>{e.preventDefault();dismiss();});dialog.addEventListener('click',e=>{if(e.target===dialog){const box=dialog.getBoundingClientRect();if(e.clientX<box.left||e.clientX>box.right||e.clientY<box.top||e.clientY>box.bottom)dismiss();}});
 });
 document.querySelectorAll('[data-portal-target]').forEach(link=>link.addEventListener('click',e=>{if(panels.has(link.dataset.portalTarget)){e.preventDefault();open(link.dataset.portalTarget);}}));
 window.addEventListener('popstate',()=>{close();const id=location.hash.slice(1);if(panels.has(id))open(id,false);});
 const initial=location.hash.slice(1)||root.dataset.startPanel;if(panels.has(initial))open(initial,false);
})();
