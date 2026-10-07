(() => {
    const role = document.querySelector('#Role');
    if (!role) return;

    function updateCoachFields() {
        const isCoach = role.value === role.dataset.coachRole;
        document.querySelectorAll('[data-coach-fields]').forEach(section => {
            section.hidden = !isCoach;
            section.querySelectorAll('input, select, textarea').forEach(field => {
                field.disabled = !isCoach;
            });
        });
    }

    role.addEventListener('change', updateCoachFields);
    updateCoachFields();
})();
