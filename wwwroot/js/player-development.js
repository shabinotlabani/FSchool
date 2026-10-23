document.querySelectorAll('[data-metric]').forEach(group => {
    const value = group.querySelector('[data-metric-value]');
    const range = group.querySelector('[data-metric-range]');
    range?.addEventListener('input', () => { value.value = range.value; value.dispatchEvent(new Event('change', { bubbles: true })); });
    value?.addEventListener('input', () => { if (range && value.value !== '' && value.validity.valid) range.value = value.value; });
    group.querySelectorAll('[data-step]').forEach(button => button.addEventListener('click', () => {
        if (button.dataset.step === '1') value.stepUp(); else value.stepDown();
        value.dispatchEvent(new Event('input', { bubbles: true }));
    }));
});
document.querySelectorAll('[data-score]').forEach(input => {
    const output = document.querySelector(`output[for="${input.id}"]`);
    const update = () => { if (output) output.value = input.value + ' / 5'; };
    input.addEventListener('input', update); update();
});
