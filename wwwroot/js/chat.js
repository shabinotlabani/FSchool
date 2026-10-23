(() => {
    const chat = document.querySelector('[data-chat]');
    if (!chat) return;
    const log = chat.querySelector('.chat-messages'), form = chat.querySelector('form'), status = chat.querySelector('.chat-connection');
    const csrf = form.querySelector('[name="__RequestVerificationToken"]').value;
    let busy = false, signature = '';
    const read = async () => {
        if (document.hidden) return;
        const ids = [...log.querySelectorAll('[data-id]')].map(x => Number(x.dataset.id));
        if (!ids.length) return;
        const body = new URLSearchParams({ __RequestVerificationToken: csrf, through: String(Math.max(...ids)) });
        const response = await fetch(chat.dataset.read, { method: 'POST', body, credentials: 'same-origin' });
        if (!response.ok) throw new Error();
    };
    async function poll() {
        if (busy || document.hidden) return;
        busy = true;
        try {
            if (chat.dataset.poll) {
                const response = await fetch(chat.dataset.poll, { cache: 'no-store', credentials: 'same-origin' });
                if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error();
                const data = await response.json(), next = JSON.stringify(data.messages);
                if (signature !== next) {
                    const atBottom = log.scrollHeight - log.scrollTop - log.clientHeight < 80;
                    if (data.messages.length) {
                        const nodes = data.messages.map(m => {
                            const article = document.createElement('article'); article.className = 'chat-message' + (m.mine ? ' mine' : ''); article.dataset.id = m.id;
                            const p = document.createElement('p'); p.textContent = m.body;
                            const small = document.createElement('small'); small.textContent = m.date + (m.mine ? (m.read ? ' · Lexuar' : ' · Dërguar') : '');
                            article.append(p, small); return article;
                        });
                        log.replaceChildren(...nodes);
                    }
                    if (atBottom || !signature) log.scrollTop = log.scrollHeight;
                    signature = next;
                }
            }
            await read(); status.textContent = '';
        } catch { status.textContent = 'Lidhja u ndërpre. Mesazhet do të rifreskohen automatikisht.'; }
        finally { busy = false; }
    }
    log.scrollTop = log.scrollHeight;
    poll(); setInterval(poll, 15000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) poll(); });
    form.addEventListener('submit', () => { form.querySelector('button').disabled = true; });
})();
