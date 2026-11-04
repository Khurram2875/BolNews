/* Poll card behaviour: Vote now dialog, casting a vote, live bar update, local deadline text. */
(function () {
    'use strict';
    if (window.__bnPollInit) return;   // the script tag is emitted once per card; run once per page
    window.__bnPollInit = true;

    function remaining(ms) {
        var s = Math.floor(ms / 1000), d = Math.floor(s / 86400), h = Math.floor((s % 86400) / 3600), m = Math.floor((s % 3600) / 60);
        if (d > 0) return d + 'd ' + h + 'h left';
        if (h > 0) return h + 'h ' + m + 'm left';
        return Math.max(1, m) + ' min left';
    }

    function markEnded(card) {
        card.classList.add('is-ended');
        var foot = card.querySelector('.bn-poll__foot');
        var action = foot.querySelector('.bn-poll__vote, .bn-poll__voted');
        if (action) {
            var final = document.createElement('span');
            final.className = 'bn-poll__final';
            final.textContent = 'Final results';
            action.replaceWith(final);   // plain text, no link, no button
        }
        var dlg = card.querySelector('dialog');
        if (dlg) { if (dlg.open) dlg.close(); dlg.remove(); }
        var label = card.querySelector('.bn-poll__deadline-label'); if (label) label.textContent = 'Ended';
        var left = card.querySelector('.bn-poll__left'); if (left) left.textContent = '';
    }

    function tick(card) {
        var end = new Date(card.dataset.ends).getTime();
        var left = card.querySelector('.bn-poll__left');
        if (Date.now() >= end) { if (!card.classList.contains('is-ended') || card.querySelector('.bn-poll__vote')) markEnded(card); return; }
        if (left) left.textContent = '· ' + remaining(end - Date.now());
    }

    function localiseDeadline(card) {
        var t = card.querySelector('.bn-poll__time');
        var d = new Date(card.dataset.ends);
        if (!t || isNaN(d)) return;
        try {
            t.textContent = d.toLocaleString(undefined, { day: '2-digit', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' });
        } catch (e) { /* keep the UTC text */ }
    }

    function applyResults(card, options) {
        var leader = 0;
        options.forEach(function (o) { if (o.percent > leader) leader = o.percent; });
        options.forEach(function (o) {
            var li = card.querySelector('[data-option-id="' + o.id + '"]');
            if (!li) return;
            li.querySelector('.bn-poll__pct').textContent = o.percent + '%';
            li.querySelector('.bn-poll__bar > span').style.width = o.percent + '%';
            li.querySelector('.bn-poll__bar').setAttribute('aria-valuenow', o.percent);
            li.classList.toggle('is-leading', leader > 0 && o.percent === leader);
        });
    }

    function markVoted(card) {
        card.classList.add('has-voted');
        var btn = card.querySelector('.bn-poll__vote');
        if (btn) {
            var span = document.createElement('span');
            span.className = 'bn-poll__voted';
            span.textContent = '✓ Thanks for voting';
            btn.replaceWith(span);
        }
    }

    document.addEventListener('click', function (e) {
        var open = e.target.closest('[data-poll-open]');
        if (open) {
            var dlg = open.closest('.bn-poll').querySelector('dialog');
            if (dlg && typeof dlg.showModal === 'function') {
                var msg = dlg.querySelector('.bn-poll__msg'); msg.hidden = true; msg.textContent = '';
                dlg.showModal();
            }
            return;
        }
        if (e.target.closest('[data-poll-close]')) { e.target.closest('dialog').close(); return; }
        // click on the dimmed backdrop closes the dialog
        if (e.target.tagName === 'DIALOG' && e.target.classList.contains('bn-poll__dialog')) e.target.close();
    });

    document.addEventListener('submit', function (e) {
        var form = e.target.closest('.bn-poll__form');
        if (!form) return;
        e.preventDefault();
        var card = form.closest('.bn-poll');
        var msg = form.querySelector('.bn-poll__msg');
        var submit = form.querySelector('.bn-poll__submit');
        submit.disabled = true;
        fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.json().catch(function () { return { ok: false, message: 'Something went wrong. Please try again.' }; }); })
            .then(function (data) {
                if (data.options && data.options.length) applyResults(card, data.options);
                if (data.ended) { markEnded(card); return; }
                if (data.ok || data.voted) {
                    markVoted(card);
                    var dlg = form.closest('dialog'); if (dlg) dlg.close();
                } else {
                    msg.textContent = data.message || 'Your vote could not be recorded.';
                    msg.hidden = false;
                }
            })
            .catch(function () { msg.textContent = 'Network problem. Please try again.'; msg.hidden = false; })
            .then(function () { submit.disabled = false; });
    });

    function init() {
        var cards = document.querySelectorAll('.bn-poll');
        cards.forEach(function (c) { localiseDeadline(c); tick(c); });
        setInterval(function () { cards.forEach(tick); }, 30000);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
