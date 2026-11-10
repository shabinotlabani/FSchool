if(new URL(location.href).searchParams.get('autoPrint')==='true'){
 window.addEventListener('load',async()=>{
  if(document.fonts?.ready)await document.fonts.ready;
  window.print();
 },{once:true});
}
