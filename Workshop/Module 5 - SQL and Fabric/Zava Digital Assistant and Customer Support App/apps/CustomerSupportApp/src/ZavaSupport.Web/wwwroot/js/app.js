(() => {
    'use strict';

    const byId = id => document.getElementById(id);
    const list = byId('escalation-list');
    const modal = byId('action-modal');
    const backdrop = byId('action-backdrop');
    const escalations = new Map();
    let selectedId = null;
    let filterTerm = '';

    const matches = escalation => {
        if (!filterTerm) return true;
        return (escalation.escalationId || '').toLowerCase().includes(filterTerm) ||
            (escalation.email || '').toLowerCase().includes(filterTerm);
    };

    const time = value => {
        const parsed = new Date(value.endsWith('Z') ? value : value + 'Z');
        return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
    };

    function setStatus(status) {
        const pill = byId('status-pill');
        pill.dataset.state = status.state;
        pill.title = status.message;
        byId('status-text').textContent = status.state === 'live' ? 'Live stream' : status.state === 'connecting' ? 'Connecting…' : 'Stream unavailable';
        byId('feed-note').textContent = status.message;
    }

    function card(escalation, isNew) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'escalation-card' + (isNew ? ' is-new' : '');
        button.dataset.id = escalation.escalationId;
        button.setAttribute('aria-current', String(escalation.escalationId === selectedId));

        const top = document.createElement('div');
        top.className = 'd-flex justify-content-between align-items-start gap-2';
        const email = document.createElement('span');
        email.className = 'fw-semibold text-break';
        email.textContent = escalation.email;
        const badge = document.createElement('span');
        badge.className = 'badge rounded-pill text-bg-light border flex-shrink-0';
        badge.textContent = escalation.messageCount + ' messages';
        top.append(email, badge);

        const meta = document.createElement('p');
        meta.className = 'text-secondary small mb-0 mt-1';
        meta.textContent = 'Requested ' + time(escalation.createdAt);

        const preview = document.createElement('p');
        preview.className = 'small text-body-secondary mb-0 mt-1 text-truncate';
        const firstUser = escalation.messages.find(message => message.role === 'user');
        preview.textContent = firstUser ? firstUser.content : '';

        button.append(top, meta, preview);
        button.addEventListener('click', () => select(escalation.escalationId));
        item.append(button);
        return item;
    }

    function render(escalation, isNew) {
        list.prepend(card(escalation, isNew));
        byId('empty-state').classList.toggle('d-none', escalations.size > 0);
        byId('count-badge').textContent = escalations.size + (escalations.size === 1 ? ' escalation' : ' escalations');
        applyFilter();
    }

    function applyFilter() {
        let visible = 0;
        escalations.forEach(escalation => {
            const item = list.querySelector('.escalation-card[data-id="' + CSS.escape(escalation.escalationId) + '"]');
            if (!item) return;
            const show = matches(escalation);
            item.parentElement.classList.toggle('d-none', !show);
            if (show) visible += 1;
        });
        const hasEscalations = escalations.size > 0;
        byId('empty-state').classList.toggle('d-none', hasEscalations);
        byId('no-match-state').classList.toggle('d-none', !hasEscalations || visible > 0);
    }

    function bubble(message) {
        const wrapper = document.createElement('div');
        wrapper.className = 'bubble ' + (message.role === 'user' ? 'bubble-user' : 'bubble-assistant');
        const meta = document.createElement('div');
        meta.className = 'bubble-meta';
        meta.textContent = (message.role === 'user' ? 'Customer' : 'Zava assistant') + ' · ' + time(message.createdAt) +
            (message.status && message.status !== 'Completed' ? ' · ' + message.status : '');
        const body = document.createElement('div');
        body.textContent = message.content;
        wrapper.append(meta, body);
        return wrapper;
    }

    function select(id) {
        const escalation = escalations.get(id);
        if (!escalation) return;
        selectedId = id;
        list.querySelectorAll('.escalation-card').forEach(button =>
            button.setAttribute('aria-current', String(button.dataset.id === id)));
        byId('detail-empty').classList.add('d-none');
        byId('detail').classList.remove('d-none');
        byId('detail-email').textContent = escalation.email;
        byId('detail-created').textContent = time(escalation.createdAt);
        byId('detail-messages').textContent = escalation.messageCount + ' messages';
        byId('detail-escalation-id').textContent = escalation.escalationId;
        byId('detail-session-id').textContent = escalation.sessionId;
        const transcript = byId('transcript');
        transcript.replaceChildren(...escalation.messages.map(bubble));
    }

    function toggleModal(open) {
        modal.classList.toggle('show', open);
        modal.classList.toggle('d-block', open);
        modal.setAttribute('aria-hidden', String(!open));
        backdrop.classList.toggle('d-none', !open);
        backdrop.classList.toggle('show', open);
        document.body.classList.toggle('modal-open', open);
        if (open) modal.querySelector('[data-close-modal]').focus();
        else byId('take-action').focus();
    }

    byId('take-action').addEventListener('click', () => {
        const escalation = escalations.get(selectedId);
        if (!escalation) return;
        byId('action-email').textContent = escalation.email;
        byId('action-reference').textContent = escalation.escalationId;
        toggleModal(true);
    });
    modal.querySelectorAll('[data-close-modal]').forEach(button => button.addEventListener('click', () => toggleModal(false)));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && modal.classList.contains('show')) toggleModal(false);
    });

    function add(escalation, isNew) {
        if (escalations.has(escalation.escalationId)) return;
        escalations.set(escalation.escalationId, escalation);
        render(escalation, isNew);
    }

    byId('search-input').addEventListener('input', event => {
        filterTerm = event.target.value.trim().toLowerCase();
        applyFilter();
    });

    const source = new EventSource('/api/escalations/stream');
    source.addEventListener('snapshot', event => {
        const payload = JSON.parse(event.data);
        setStatus(payload.status);
        payload.escalations.slice().reverse().forEach(escalation => add(escalation, false));
    });
    source.addEventListener('escalation', event => add(JSON.parse(event.data), true));
    source.addEventListener('status', event => setStatus(JSON.parse(event.data)));
    source.addEventListener('error', () => setStatus({ state: 'error', message: 'Reconnecting to the escalation stream.' }));
})();
