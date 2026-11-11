/* Urdu newsroom support for the admin area (loaded only when the site language is Urdu).
   The admin UI itself stays left-to-right; only the text a writer types switches direction. */
(function () {
    'use strict';
    var FIELD = 'input[type="text"]:not([readonly]), input:not([type]), input[type="search"], textarea';
    var SKIP = /(slug|url|email|password|canonical|keyword|code|embed)/i;

    function markField(el) {
        if (el.dataset.urduReady) return;
        var key = (el.name || '') + ' ' + (el.id || '');
        if (SKIP.test(key) || el.closest('.ql-container')) return;
        el.dataset.urduReady = '1';
        el.setAttribute('dir', 'auto'); // follows the first strong character: Urdu right-to-left, Latin left-to-right
    }

    function markEditors(scope) {
        scope.querySelectorAll('.ql-editor').forEach(function (ed) {
            if (ed.dataset.urduReady) return;
            ed.dataset.urduReady = '1';
            ed.setAttribute('dir', 'rtl');
            ed.classList.add('bn-urdu-editor');
        });
    }

    function scan(scope) {
        scope.querySelectorAll(FIELD).forEach(markField);
        markEditors(scope);
    }

    document.addEventListener('DOMContentLoaded', function () {
        scan(document);
        // Editors and dynamically added rows (live event updates, poll options) appear after load.
        new MutationObserver(function (records) {
            records.forEach(function (r) {
                r.addedNodes.forEach(function (n) { if (n.nodeType === 1) { scan(n); if (n.matches && n.matches(FIELD)) markField(n); } });
            });
        }).observe(document.body, { childList: true, subtree: true });
    });
})();
