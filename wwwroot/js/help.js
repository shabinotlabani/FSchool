(() => {
    'use strict';
    const drawer = document.getElementById('context-help');
    if (!drawer || drawer.dataset.ready) return;
    drawer.dataset.ready = 'true';
    const tour = document.getElementById('help-tour');
    const formList = document.getElementById('help-form-list');
    const formsSeen = new WeakSet();
    const byId = id => document.getElementById(id);
    let trigger = null, fields = [], current = 0, marked = null, formTrigger = null;
    const openedDetails = new Set();
    const hints = {
        Reason: 'Shpjegoni pse po bëhet ky veprim. Përdorni një arsye konkrete që kuptohet edhe kur lexohet më vonë në historik.',
        Notes: 'Shtoni vetëm sqarimet që lidhen me këtë regjistrim.',
        ExemptionType: 'Zgjidhni Sëmundje, Pushime ose Arsye tjetër. Përshkrimi i arsyes mbetet i detyrueshëm.',
        EffectiveMonth: 'Ky është muaji nga i cili do të vlejë ndryshimi. Ndryshimi i kategorisë nuk rishkruan faturat që ekzistojnë.',
        FeePlanId: 'Kategoria përcakton tarifën. Kontrolloni emrin dhe çmimin; paketa familjare menaxhohet sipas përbërjes së familjes.',
        PlayersOnCourt: 'Numri përfshin portierin: 8+1 = 9, 6+1 = 7. Te ndeshjet e ligës merret nga rregulli i ligës.',
        Formation: 'Zgjidhni vendosjen e lojtarëve. Për formatet me më pak se 11 përdoret vendosja sipas ekipit.',
        LeagueId: 'Zgjidhni garën ku merr pjesë ekipi në datën e ndeshjes. Kontrolloni sezonin dhe formatin e lojës.',
        TrainingTeamId: 'Zgjidhni ekipin e lojtarit. Kontrolloni orarin dhe kapacitetin që shfaqet.',
        TeamId: 'Kontrolloni ekipin për të cilin po kryeni veprimin.',
        StudentId: 'Gjeni dhe zgjidhni lojtarin e saktë. Kontrolloni emrin dhe ekipin para se të vazhdoni.',
        FamilyId: 'Zgjidhni familjen e duhur dhe kontrolloni anëtarët e saj.',
        PaymentMethod: 'Zgjidhni mënyrën reale të pagesës. Në formularët e verifikimit bankar, Bankë kalon fillimisht në pritje.',
        PaymentDate: 'Shënoni datën kur është kryer pagesa. Kontrolloni kufirin e datës që lejon formulari.',
        Amount: 'Vendosni shumën e këtij veprimi dhe kontrolloni totalin para ruajtjes.',
        DateOfBirth: 'Vendosni datëlindjen reale. Ajo përdoret në kartelën e lojtarit dhe në kontrollet e ekipit.',
        ParentPhone: 'Shënoni numrin e kontaktit të prindit ose kujdestarit, me prefiksin e shtetit kur duhet.',
        ParentEmail: 'Kontrolloni emailin e prindit ose kujdestarit për të shmangur gabimet në kontakt.',
        Email: 'Shkruani emailin e saktë. Te llogaritë e stafit, ai përdoret për hyrje.',
        Role: 'Zgjidhni rolin e duhur. Në llogaritë e stafit roli përcakton qasjen dhe veprimet e lejuara.',
        IsActive: 'Kontrolloni nëse ky regjistrim duhet të jetë aktiv. Çaktivizimi i lojtarit nuk fshin borxhet ekzistuese.',
        Quantity: 'Vendosni sasinë për variantin ose madhësinë e zgjedhur dhe kontrolloni stokun.',
        ProductId: 'Zgjidhni rekuizitën; pastaj kontrolloni madhësinë dhe sasinë përkatëse.',
        SizeId: 'Zgjidhni madhësinë e saktë. Stoku mund të ndryshojë nga një madhësi në tjetrën.',
        MatchGameId: 'Zgjidhni ndeshjen me të cilën lidhet ky regjistrim. Kontrolloni kundërshtarin dhe datën.',
        Opponent: 'Shkruani emrin e kundërshtarit që të dallohet në kalendar dhe raporte.',
        Duration: 'Shënoni kohëzgjatjen në minuta. Hyrjet dhe daljet e lojtarëve duhet të jenë brenda kësaj kohe.',
        From: 'Kontrolloni fillimin e periudhës ose intervalit që tregon etiketa.',
        To: 'Kontrolloni fundin e periudhës ose intervalit; ai duhet të përputhet me fillimin.',
        CoachRating: 'Zgjidhni vlerësimin sipas shkallës së paraqitur për këtë lojtar dhe ndeshje.',
        CoachNote: 'Shkruani vërejtje konkrete për paraqitjen e lojtarit në këtë ndeshje.',
        confirmation: 'Ky tekst konfirmon një veprim të ndjeshëm. Lexoni ndikimin e veprimit para se ta plotësoni.'
    };
    function label(field) {
        const labels = [...(field.labels || [])];
        const source = labels.find(l => l.textContent.trim());
        let text = '';
        if (source) {
            const copy = source.cloneNode(true);
            copy.querySelectorAll('input,select,textarea,button,small,[data-valmsg-for]').forEach(n => n.remove());
            text = copy.textContent;
        }
        return (text || field.getAttribute('aria-label') || field.getAttribute('placeholder') ||
            (field.name || 'Fusha').split('.').pop().replace(/([a-z])([A-Z])/g, '$1 $2'))
            .replace(/\s+/g, ' ').replace(/\*\s*$/, '').trim().slice(0, 150);
    }
    function eligible(field) {
        if (['hidden','submit','button','reset','image'].includes(field.type) || field.disabled || field.closest('[data-help-owned],[hidden],[aria-hidden="true"]')) return false;
        if (field.closest('dialog') && !field.closest('dialog').open) return false;
        for (let node = field; node && node !== field.form; node = node.parentElement) {
            const style = getComputedStyle(node);
            if (style.display === 'none' || style.visibility === 'hidden') return false;
        }
        return true;
    }
    const formFields = form => [...form.querySelectorAll('input,select,textarea')].filter(eligible);
    function title(form, index) {
        const heading = form.querySelector('legend,h2,h3');
        // Button captions identify forms without reading user-entered values.
        const submit = form.querySelector('button[type=submit],button:not([type])');
        return (form.getAttribute('aria-label') || submit?.textContent || heading?.textContent || `Formulari ${index+1}`).replace(/\s+/g,' ').trim().slice(0,100);
    }
    function availableForms() {
        return [...document.forms].filter(f => !f.closest('[data-help-owned],.app-sidebar,.club-footer') && formFields(f).length > 0);
    }
    function refreshForms() {
        const forms = availableForms();
        byId('help-form-tools').hidden = forms.length === 0;
        formList.replaceChildren();
        forms.forEach((form, index) => {
            const row = document.createElement('div'); row.className = 'help-form-row';
            const name = document.createElement('strong'); name.textContent = title(form,index);
            const count = document.createElement('small'); count.textContent = `${formFields(form).length} fusha në këtë formular`;
            const info = document.createElement('div'); info.append(name,count);
            const start = document.createElement('button'); start.type = 'button'; start.textContent = 'Më udhëzo';
            start.onclick = () => startTour(form, trigger); row.append(info,start); formList.append(row);
        });
    }
    function explain(field) {
        const key = (field.name || '').split('.').pop().replace(/\[.*?\]/g, '');
        let hint = hints[key];
        const action = (field.form?.getAttribute('action') || location.pathname).toLowerCase();
        if (key === 'Amount' && action.includes('correctfee')) hint = 'Shkruani tarifën përfundimtare, jo diferencën. Shembull: fatura 50 dhe pagesa 30 → shkruani 30 që borxhi të bëhet 0.';
        if (key === 'Amount' && action.includes('exempt')) hint = 'Shkruani shumën që falet, jo tarifën e re. Shembull: nga fatura 50 falen 20 → mbeten 30 për mbulim.';
        if (field.type === 'password') hint = 'Plotësojeni vetë këtë fushë. Udhëzuesi nuk lexon, ruan ose dërgon fjalëkalimin.';
        if (!hint) hint = field.tagName === 'SELECT' ? 'Zgjidhni opsionin që përputhet me këtë regjistrim.' :
            field.type === 'checkbox' ? 'Aktivizojeni vetëm nëse dëshironi të zbatohet opsioni i përshkruar.' :
            field.type === 'radio' ? 'Zgjidhni këtë alternativë nëse përputhet me veprimin që dëshironi.' :
            field.type === 'file' ? 'Zgjidhni skedarin nga pajisja. Kontrolloni llojin e lejuar dhe udhëzimet pranë fushës.' :
            field.type === 'date' || field.type === 'month' ? 'Zgjidhni datën ose muajin për këtë regjistrim dhe kontrolloni periudhën.' :
            field.type === 'number' ? 'Vendosni vlerën sipas njësisë dhe përshkrimit të fushës.' :
            'Plotësoni informacionin që kërkon kjo fushë dhe kontrolloni saktësinë para ruajtjes.';
        const constraints = [];
        if (field.readOnly) constraints.push('Kjo fushë caktohet nga sistemi dhe nuk ndryshohet këtu.');
        else if (field.required || field.getAttribute('aria-required') === 'true' || field.type !== 'checkbox' && field.hasAttribute('data-val-required')) constraints.push('Fushë e detyrueshme.');
        for (const [attr, name] of [['min','Minimumi'],['max','Maksimumi'],['maxlength','Gjatësia maksimale']]) {
            const limit = field.getAttribute(attr);
            if (limit && limit.length < 40) constraints.push(`${name}: ${limit}${attr==='maxlength'?' karaktere':''}.`);
        }
        return hint + (constraints.length ? '\n\n' + constraints.join(' ') : '');
    }
    function stopTour(restore=true) {
        marked?.classList.remove('help-field-highlight'); marked = null;
        tour.hidden = true;
        openedDetails.forEach(d => d.open = false); openedDetails.clear();
        document.body.append(tour);
        if (restore && formTrigger?.isConnected) formTrigger.focus({preventScroll:true});
    }
    function showStep() {
        marked?.classList.remove('help-field-highlight');
        const field = fields[current];
        if (!field?.isConnected || !eligible(field)) { stopTour(); return; }
        for (let d = field.closest('details'); d; d = d.parentElement?.closest('details')) {
            if (!d.open) { openedDetails.add(d); d.open = true; }
        }
        const host = field.closest('dialog[open]');
        (host || document.body).append(tour);
        marked = field; field.classList.add('help-field-highlight');
        byId('help-tour-count').textContent = `FUSHA ${current+1} NGA ${fields.length}`;
        byId('help-tour-title').textContent = label(field);
        byId('help-tour-text').textContent = explain(field);
        byId('help-tour-prev').disabled = current === 0;
        byId('help-tour-next').textContent = current === fields.length-1 ? 'Përfundo ✓' : 'Vazhdo →';
        tour.hidden = false;
        field.scrollIntoView({block:'center',behavior:'instant'});
        byId('help-tour-next').focus({preventScroll:true});
    }
    function startTour(form, origin) {
        stopTour(false); formTrigger = origin; fields = formFields(form); current = 0;
        if (!fields.length) return;
        drawer.close(); showStep();
    }
    function enhanceForms() {
        availableForms().forEach((form,index) => {
            if (formsSeen.has(form)) return;
            formsSeen.add(form);
            const button = document.createElement('button'); button.type = 'button';
            button.className = 'help-inline-button'; button.dataset.helpOwned = '';
            button.textContent = 'ⓘ Më udhëzo'; button.setAttribute('aria-label',`Më udhëzo: ${title(form,index)}`);
            button.onclick = () => startTour(form,button); form.prepend(button);
        });
    }
    document.querySelectorAll('[data-help-open]').forEach(button => button.addEventListener('click', () => {
        stopTour(false); trigger = button; refreshForms(); drawer.showModal(); drawer.querySelector('[data-help-close]').focus();
    }));
    drawer.querySelector('[data-help-close]').onclick = () => drawer.close();
    drawer.addEventListener('close', () => { if(tour.hidden)trigger?.focus({preventScroll:true}); });
    drawer.addEventListener('click', event => { if(event.target===drawer && event.clientX < drawer.getBoundingClientRect().left) drawer.close(); });
    byId('help-tour-close').onclick = () => stopTour();
    byId('help-tour-prev').onclick = () => { if(current>0){current--;showStep();} };
    byId('help-tour-next').onclick = () => { if(current<fields.length-1){current++;showStep();}else stopTour(); };
    document.addEventListener('keydown', event => { if(event.key==='Escape' && !tour.hidden){event.preventDefault();stopTour();} });
    // End guidance if the user submits a form. Never prevent or perform submission.
    document.addEventListener('submit', () => stopTour(false), true);
    enhanceForms();
    document.addEventListener('click', event => {
        if(!event.target.closest('[data-help-owned]'))requestAnimationFrame(enhanceForms);
    });
    let queued=false;
    new MutationObserver(records => {
        if(queued || !records.some(r => [...r.addedNodes].some(n=>n.nodeType===1 && !n.closest?.('[data-help-owned]') && (n.matches?.('form') || n.querySelector?.('form')))))return;
        queued=true; requestAnimationFrame(()=>{queued=false;enhanceForms();});
    }).observe(document.body,{childList:true,subtree:true});
})();
