// ── Global site initialization ───────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    // Sticky navbar
    const nav = document.getElementById('sticky-navbar');
    const body = document.body;
    const stickyTrigger = 150;

    if (nav) {
        const toggle = nav.querySelector('.bn-nav-toggle');

        function syncStickyNavbar() {
            const shouldFix = window.scrollY > stickyTrigger;
            nav.classList.toggle('fixed-nav', shouldFix);
            body.classList.toggle('nav-fixed', shouldFix);
        }

        window.addEventListener('scroll', syncStickyNavbar, { passive: true });
        syncStickyNavbar();

        if (toggle) {
            toggle.setAttribute('aria-expanded', 'false');
            toggle.setAttribute('aria-controls', 'publicNavbarMenu');
            nav.querySelector('.bn-menu')?.setAttribute('id', 'publicNavbarMenu');

            toggle.addEventListener('click', function () {
                const isOpen = nav.classList.toggle('bn-nav-open');
                toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
            });

            nav.querySelectorAll('.bn-menu-link').forEach(function (link) {
                link.addEventListener('click', function () {
                    nav.classList.remove('bn-nav-open');
                    toggle.setAttribute('aria-expanded', 'false');
                });
            });

            document.addEventListener('click', function (e) {
                if (!nav.contains(e.target)) {
                    nav.classList.remove('bn-nav-open');
                    toggle.setAttribute('aria-expanded', 'false');
                }
            });

            nav.querySelectorAll('.bn-menu-item.dropdown').forEach(function (item) {
                const caret = item.querySelector('.bn-menu-caret');
                if (!caret) return;

                caret.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    const isOpen = item.classList.toggle('is-open');
                    caret.classList.toggle('is-open', isOpen);
                    caret.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

                    nav.querySelectorAll('.bn-menu-item.dropdown.is-open').forEach(function (other) {
                        if (other !== item) {
                            other.classList.remove('is-open');
                            other.querySelector('.bn-menu-caret')?.classList.remove('is-open');
                            other.querySelector('.bn-menu-caret')?.setAttribute('aria-expanded', 'false');
                        }
                    });
                });
            });
        }
    }

    // Analytics click tracking
    document.querySelectorAll('.track-click').forEach(function (link) {
        link.addEventListener('click', function () {
            const id = this.dataset.articleId;
            if (id) {
                navigator.sendBeacon('/analytics/click/' + id);
            }
        });
    });

    // Trending tabs
    document.querySelectorAll('.trend-tab').forEach(function (tab) {
        tab.addEventListener('click', function (e) {
            e.preventDefault();

            const type = this.dataset.type;
            document.querySelectorAll('.trend-tab').forEach(function (t) {
                t.classList.remove('active');
            });
            this.classList.add('active');

            fetch('/Home/GetTrending?type=' + encodeURIComponent(type || ''))
                .then(function (res) { return res.text(); })
                .then(function (html) {
                    const container = document.getElementById('trending-container');
                    if (container) {
                        container.innerHTML = html;
                    }
                });
        });
    });
});

function trackImpression(articleId) {
    fetch('/analytics/impression/' + articleId, { method: 'POST' });
}
