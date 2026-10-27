(() => {
 const type=document.getElementById('PersonType'),person=document.getElementById('BeneficiaryId');
 if(!type||!person)return;
 const staff=type.value==='Staff';
 document.getElementById('contract-waiver').hidden=staff;
 person.addEventListener('change',()=>{if(staff)document.getElementById('Terms_PersonalNumber').value=person.selectedOptions[0]?.dataset.identifier||'';});
})();
