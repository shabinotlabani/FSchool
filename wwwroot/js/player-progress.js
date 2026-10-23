document.querySelectorAll('.progress-chart').forEach(chart => {
    const readout = chart.querySelector('.plot-readout');
    chart.querySelectorAll('.plot-hit').forEach(point => {
        const show = () => { readout.textContent = point.dataset.value; };
        point.addEventListener('focus', show);
        point.addEventListener('click', show);
        point.addEventListener('pointerenter', show);
        point.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); show(); }
        });
    });
});
if (window.location.hash === '#menu') {
    const menu = document.getElementById('menu');
    if (menu) { menu.open = true; menu.scrollIntoView({ block: 'start' }); }
}
const metricPicker = document.getElementById('progress-metric');
if (metricPicker) {
    const update = () => document.querySelectorAll('[data-progress-panel]').forEach(panel => {
        panel.hidden = panel.dataset.progressPanel !== metricPicker.value;
    });
    metricPicker.addEventListener('change', update);
    update();
    document.querySelector('.progress-picker').hidden = false;
}

if (['#injuries', '#doctor', '#psychologist'].includes(window.location.hash)) { const section = document.getElementById(window.location.hash.slice(1)); if (section) section.open = true; }
