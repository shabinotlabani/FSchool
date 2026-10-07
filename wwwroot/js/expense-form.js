(() => {
    const parent=document.querySelector('#CategoryId'), child=document.querySelector('#SubcategoryId');
    if(!parent||!child)return;
    const categories=JSON.parse(document.querySelector('#expense-categories').textContent);
    function update(keep) {
        const previous=keep?child.value:'';
        child.replaceChildren(new Option('Pa nënkategori',''));
        const choices=categories.filter(c=>String(c.parentId)===parent.value);
        for(const c of choices)child.add(new Option(c.name,String(c.id)));
        child.value=choices.some(c=>String(c.id)===previous)?previous:'';
    }
    parent.addEventListener('change',()=>update(false));
    update(true);
})();