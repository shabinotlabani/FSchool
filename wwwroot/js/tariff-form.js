(() => {
    const type = document.querySelector('#IsFamily');
    const fields = document.querySelector('#family-tariff-fields');
    function update() {
        const family = type.value.toLowerCase() === 'true';
        fields.hidden = !family;
        fields.querySelectorAll('input').forEach(input => {
            input.disabled = !family;
            input.required = family && input.type !== 'checkbox';
        });
    }
    type.addEventListener('change', update);
    update();
})();
