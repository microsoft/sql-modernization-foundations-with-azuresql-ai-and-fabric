(() => {
    'use strict';
    const byId = id => document.getElementById(id);
    const dialog = byId('chat-dialog');
    const input = byId('chat-input');
    const messages = byId('chat-messages');
    const email = byId('followup-email');
    let state;

    function newState() {
        return {
            sessionId: crypto.randomUUID(),
            key: Array.from(crypto.getRandomValues(new Uint8Array(32)), value => value.toString(16).padStart(2, '0')).join(''),
            pending: null, hasMessage: false, busy: false, closed: false, limit: false, escalation: null
        };
    }

    function reset() {
        state = newState();
        messages.replaceChildren();
        input.value = '';
        email.value = '';
        dialog.classList.remove('followup-open', 'closed');
        byId('chat-welcome').hidden = false;
        for (const id of ['followup-section', 'followup-success', 'chat-error', 'chat-progress']) byId(id).hidden = true;
        byId('followup-reference').textContent = '';
        renderControls();
    }

    function renderControls() {
        const formOpen = !byId('followup-section').hidden;
        input.disabled = state.busy || state.closed || state.limit || !!state.pending;
        byId('send-message').disabled = input.disabled || !input.value.trim();
        byId('open-followup').disabled = state.busy || state.closed || !state.hasMessage || formOpen || !!state.escalation;
        messages.querySelectorAll('[data-chat-handoff]').forEach(button => {
            button.disabled = byId('open-followup').disabled;
        });
        byId('new-chat').disabled = state.busy;
        byId('submit-followup').disabled = state.busy;
        byId('cancel-followup').disabled = state.busy || !!state.escalation;
        email.disabled = state.busy || !!state.escalation;
        byId('retry-message').disabled = state.busy;
        byId('character-count').textContent = `${input.value.length} / 1,200`;
        byId('chat-progress').hidden = !state.busy;
    }

    function scrollToEnd() {
        const scroller = byId('chat-scroll');
        scroller.scrollTop = scroller.scrollHeight;
    }

    function addMessage(role, text) {
        const article = document.createElement('article');
        article.className = `message ${role}`;
        const label = document.createElement('div');
        label.className = 'message-label';
        label.textContent = role === 'user' ? 'You' : 'Zava / AI';
        const status = document.createElement('span');
        status.className = 'message-status';
        label.append(status);
        const body = document.createElement('div');
        body.className = 'message-content';
        body.textContent = text;
        article.append(label, body);
        messages.append(article);
        byId('chat-welcome').hidden = true;
        scrollToEnd();
        return { article, status };
    }

    function showError(message, canRetry) {
        byId('chat-error-text').textContent = message;
        byId('chat-error').hidden = false;
        byId('retry-message').hidden = !canRetry;
        scrollToEnd();
    }

    async function post(path, body, current) {
        const response = await fetch(`/api/chat/${path}`, {
            method: 'POST', credentials: 'same-origin', cache: 'no-store',
            headers: {
                'Content-Type': 'application/json', 'X-Chat-Key': current.key,
                'X-CSRF-TOKEN': document.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(body)
        });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) {
            const failure = new Error(result.message || 'The request could not be confirmed. Retry, or refresh the page to start a new chat.');
            failure.code = result.code;
            throw failure;
        }
        return result;
    }

    async function sendMessage() {
        if (state.busy || state.closed || state.limit) return;
        if (!state.pending) {
            const text = input.value.trim();
            if (!text || text.length > 1200) return;
            state.pending = { requestId: crypto.randomUUID(), message: text, display: addMessage('user', text) };
            state.hasMessage = true;
            input.value = '';
        }
        const current = state;
        const pending = current.pending;
        current.busy = true;
        pending.display.status.textContent = 'Saving...';
        byId('chat-error').hidden = true;
        byId('chat-progress').textContent = 'Preparing and saving your reply...';
        renderControls();
        scrollToEnd();
        try {
            const result = await post('messages', { sessionId: current.sessionId, requestId: pending.requestId, message: pending.message }, current);
            if (state !== current) return;
            pending.display.status.textContent = 'Saved';
            const reply = addMessage('assistant', result.reply);
            if (result.sources?.length) {
                const links = document.createElement('div');
                links.className = 'message-sources';
                for (const source of result.sources) {
                    if (!source.url.startsWith('/#')) continue;
                    const link = document.createElement('a');
                    link.href = source.url;
                    link.textContent = `Source: ${source.title}`;
                    links.append(link);
                }
                reply.article.append(links);
            } else if (Array.isArray(result.sources) && result.sources.length === 0) {
                const handoff = document.createElement('button');
                handoff.type = 'button';
                handoff.className = 'btn btn-outline-zava chat-handoff';
                handoff.dataset.chatHandoff = '';
                const icon = document.createElement('i');
                icon.dataset.lucide = 'user-round';
                icon.setAttribute('aria-hidden', 'true');
                handoff.append(icon, document.createTextNode('Chat with human'));
                handoff.addEventListener('click', openFollowup);
                reply.article.append(handoff);
                window.lucide?.createIcons();
            }
            current.pending = null;
        } catch (error) {
            if (state !== current) return;
            if (error.code === 'conversation_limit') {
                pending.display.article.remove();
                current.pending = null;
                current.limit = true;
                showError(error.message, false);
            } else {
                pending.display.status.textContent = 'Reply not confirmed';
                showError(error.message || 'Connection interrupted. Retry this message.', true);
            }
        } finally {
            current.busy = false;
            if (state === current) { renderControls(); scrollToEnd(); if (!current.pending) input.focus(); }
        }
    }

    document.querySelectorAll('[data-open-chat]').forEach(button => button.addEventListener('click', () => {
        if (!dialog.open) dialog.showModal();
        if (button.dataset.topic && !state.hasMessage) { input.value = button.dataset.topic; renderControls(); }
        input.focus();
    }));
    document.querySelectorAll('[data-suggestion]').forEach(button => button.addEventListener('click', () => {
        input.value = button.dataset.suggestion;
        renderControls();
        input.focus();
    }));
    byId('close-chat').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', reset);
    window.addEventListener('pageshow', event => { if (event.persisted) { if (dialog.open) dialog.close(); reset(); } });
    byId('new-chat').addEventListener('click', () => { reset(); input.focus(); });
    byId('success-new-chat').addEventListener('click', () => { reset(); input.focus(); });
    input.addEventListener('input', renderControls);
    input.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) { event.preventDefault(); sendMessage(); }
    });
    byId('message-form').addEventListener('submit', event => { event.preventDefault(); sendMessage(); });
    byId('retry-message').addEventListener('click', sendMessage);
    function openFollowup() {
        if (byId('open-followup').disabled) return;
        byId('followup-section').hidden = false;
        byId('chat-error').hidden = true;
        dialog.classList.add('followup-open');
        renderControls();
        scrollToEnd();
        email.focus();
    }
    byId('open-followup').addEventListener('click', openFollowup);
    byId('cancel-followup').addEventListener('click', () => {
        byId('followup-section').hidden = true;
        dialog.classList.remove('followup-open');
        email.value = '';
        if (state.pending) showError('The previous reply is not confirmed. Retry this message or start a new chat.', true);
        renderControls();
        input.focus();
    });
    byId('followup-form').addEventListener('submit', async event => {
        event.preventDefault();
        if (state.busy || state.closed || !event.target.reportValidity()) return;
        const current = state;
        current.escalation ??= { requestId: crypto.randomUUID(), email: email.value.trim() };
        current.busy = true;
        byId('chat-error').hidden = true;
        byId('chat-progress').textContent = 'Saving your follow-up request...';
        renderControls();
        try {
            const result = await post('escalations', { sessionId: current.sessionId, ...current.escalation }, current);
            if (state !== current) return;
            current.closed = true;
            current.pending = null;
            byId('followup-section').hidden = true;
            byId('followup-success').hidden = false;
            byId('followup-reference').textContent = result.escalationId;
            dialog.classList.remove('followup-open');
            dialog.classList.add('closed');
        } catch (error) {
            if (state !== current) return;
            if (['email', 'pending', 'session', 'empty'].includes(error.code)) current.escalation = null;
            showError(error.message || 'Connection interrupted. Retry Request follow-up to confirm the saved result.', false);
        } finally {
            current.busy = false;
            if (state === current) { renderControls(); scrollToEnd(); }
        }
    });
    reset();
    window.lucide?.createIcons();
})();