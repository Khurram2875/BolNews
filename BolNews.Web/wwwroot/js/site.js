// ── Sticky Navbar ────────────────────────────────────────────────
// Navbar sits in-flow inside the header. Once the user scrolls past
// the header (150px), it snaps fixed to the top of the viewport.
// body.nav-fixed adds padding-top so content doesn't jump.

document.addEventListener('DOMContentLoaded', function () {
    const nav = document.getElementById('sticky-navbar');
    const toggle = nav?.querySelector('.bn-nav-toggle');
    const body = document.body;
    const stickyTrigger = 150;

    if (!nav) {
        return;
    }

    function syncStickyNavbar() {
        const shouldFix = window.scrollY > stickyTrigger;

        nav.classList.toggle('fixed-nav', shouldFix);
        body.classList.toggle('nav-fixed', shouldFix);
    }

    window.addEventListener('scroll', syncStickyNavbar, { passive: true });
    syncStickyNavbar();

    if (!toggle) return;

    toggle.setAttribute('aria-expanded', 'false');
    toggle.setAttribute('aria-controls', 'publicNavbarMenu');

    nav.querySelector('.bn-menu')?.setAttribute('id', 'publicNavbarMenu');

    toggle.addEventListener('click', function () {
        const isOpen = nav.classList.toggle('bn-nav-open');
        toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    });

    // Close the menu after tapping a link
    nav.querySelectorAll('.bn-menu-link').forEach(function (link) {
        link.addEventListener('click', function () {
            nav.classList.remove('bn-nav-open');
            toggle.setAttribute('aria-expanded', 'false');
        });
    });

    // Close the menu when tapping outside of it
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

            // Close any other open dropdown so only one is expanded at a time
            nav.querySelectorAll('.bn-menu-item.dropdown.is-open').forEach(function (other) {
                if (other !== item) {
                    other.classList.remove('is-open');
                    other.querySelector('.bn-menu-caret')?.classList.remove('is-open');
                    other.querySelector('.bn-menu-caret')?.setAttribute('aria-expanded', 'false');
                }
            });
        });
    });

});



// ── Analytics click tracking ─────────────────────────────────────
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".track-click").forEach(link => {
        link.addEventListener("click", function () {
            const id = this.dataset.articleId;
            if (!id) return;
            navigator.sendBeacon('/analytics/click/' + id);
        });
    });
});
document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".trend-tab").forEach(tab => {

        tab.addEventListener("click", function (e) {
            e.preventDefault();

            const type = this.dataset.type;

            // 🔥 Update active tab
            document.querySelectorAll(".trend-tab").forEach(t => t.classList.remove("active"));
            this.classList.add("active");

            // 🔥 Fetch new data
            fetch(`/Home/GetTrending?type=${type}`)
                .then(res => res.text())
                .then(html => {
                    document.getElementById("trending-container").innerHTML = html;
                });
        });

    });

});

function trackImpression(articleId) {
    fetch('/analytics/impression/' + articleId, { method: 'POST' });
}


