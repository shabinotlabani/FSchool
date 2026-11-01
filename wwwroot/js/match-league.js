(() => {
    const league = document.getElementById('LeagueId');
    const date = document.getElementById('Date');
    const stage = document.getElementById('CompetitionStage');
    const form = document.getElementById('match-form');
    const capacity = form?.querySelector('[name=PlayersOnCourt]');
    function syncCapacity() {
        if (!capacity || form.dataset.stage !== 'Draft') return;
        const total = league.selectedOptions[0]?.dataset.players;
        capacity.readOnly = !!total;
        if (total) capacity.value = total;
        const formation = document.getElementById('Formation');
        if (formation && Number(capacity.value) !== 11) formation.value = 'Auto';
        capacity.dispatchEvent(new Event('change', { bubbles: true }));
    }
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
        syncCapacity();
    }
    date.addEventListener('change', update);
    league.addEventListener('change', () => {
        const value = league.selectedOptions[0]?.dataset.stage;
        if (stage && value) stage.value = value;
        syncCapacity();
    });
    // The server preselects a league for new matches. Do not rewrite a saved
    // legacy or friendly match merely because an eligible league now exists.
})();
