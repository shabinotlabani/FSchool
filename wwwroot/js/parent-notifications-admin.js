(() => {
    const form = document.getElementById('parent-notification-form');
    if (!form) return;
    const team = form.querySelector('[name="Input.TeamId"]');
    const student = form.querySelector('[name="Input.StudentId"]');
    team.addEventListener('change', () => { if (team.value) student.value = ''; });
    student.addEventListener('change', () => { if (student.value) team.value = ''; });
    form.addEventListener('submit', event => {
        const audience = student.value ? student.selectedOptions[0].text : team.value ? team.selectedOptions[0].text : 'të gjithë prindërit';
        if (!window.confirm(`Të dërgohet ky njoftim për ${audience}?`)) event.preventDefault();
        else form.querySelector('button').disabled = true;
    });
})();
