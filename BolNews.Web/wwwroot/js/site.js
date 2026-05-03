// ── Sticky Navbar ────────────────────────────────────────────────
// Navbar sits in-flow inside the header. Once the user scrolls past
// the header (150px), it snaps fixed to the top of the viewport.
// body.nav-fixed adds padding-top so content doesn't jump.

window.onscroll = function () {
    const nav = document.getElementById("sticky-navbar");
    const body = document.body;
    const stickyTrigger = 150;

    if (window.pageYOffset > stickyTrigger) {
        nav.classList.add("fixed-nav");
        body.classList.add("nav-fixed");
    } else {
        nav.classList.remove("fixed-nav");
        body.classList.remove("nav-fixed");
    }
};

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