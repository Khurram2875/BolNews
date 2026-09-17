(function () {
    'use strict';

    window.bnSetTheme = function (name, btn) {
        if (!name) return;

        document.documentElement.setAttribute('data-theme', name);
        document.querySelectorAll('.bn-theme-btn').forEach(function (button) {
            button.classList.remove('active');
        });

        if (btn) {
            btn.classList.add('active');
        }

        try {
            localStorage.setItem('bolnews-theme', name);
        } catch (e) { }
    };

    document.addEventListener('DOMContentLoaded', function () {
        try {
            var saved = localStorage.getItem('bolnews-theme') || 'default';
            var btn = document.querySelector('.bn-theme-btn[data-t="' + saved + '"]');

            if (btn) {
                document.querySelectorAll('.bn-theme-btn').forEach(function (button) {
                    button.classList.remove('active');
                });
                btn.classList.add('active');
            }
        } catch (e) { }
    });
})();
