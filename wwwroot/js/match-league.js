(() => {
    const league = document.getElementById('LeagueId');
    const date = document.getElementById('Date');
    if (!league || !date) return;
    function update() {
        for (const option of league.options) {
            if (!option.value) continue;
            const valid = date.value >= option.dataset.start && date.value <= option.dataset.end;
            option.hidden = !valid;
            option.disabled = !valid;
        }
        if (league.selectedOptions[0]?.disabled) league.value = '';
        const eligible = [...league.options].filter(o => o.value && !o.disabled);
        if (!league.value && eligible.length === 1) league.value = eligible[0].value;
    }
    date.addEventListener('change', update);
    // The server preselects a league for new matches. Do not rewrite a saved
    // legacy or friendly match merely because an eligible league now exists.
})();
