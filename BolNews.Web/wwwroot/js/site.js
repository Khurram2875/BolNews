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

    initializeConfiguredAdPlacements();
    initializeCollapsibleAdSlots();
});

function trackImpression(articleId) {
    fetch('/analytics/impression/' + articleId, { method: 'POST' });
}

function initializeConfiguredAdPlacements() {
    const allowedPositions = ['beforebegin', 'afterbegin', 'beforeend', 'afterend'];
    document.querySelectorAll('.bn-ad-slot[data-target-selector]').forEach(function (shell) {
        const selector = shell.dataset.targetSelector;
        if (!selector) return;

        try {
            const target = document.querySelector(selector);
            const position = shell.dataset.targetPosition || 'beforeend';
            if (!target || target === shell || shell.contains(target) || !allowedPositions.includes(position)) return;
            target.insertAdjacentElement(position, shell);
        } catch (error) {
            console.warn('Invalid configured ad target selector.', selector);
        }
    });
}

function initializeCollapsibleAdSlots() {
    const adSelector =
        '[data-ad-slot]';

    const watchedAdRoots =
        new WeakSet();

    function registerAdRoot(adRoot) {
        if (!adRoot || watchedAdRoots.has(adRoot)) {
            return;
        }

        watchedAdRoots.add(adRoot);

        const shell =
            adRoot.closest('.bn-ad-slot, .bn-ad-code-block') || adRoot;

        shell.classList.add('bn-ad-collapsible');

        let hasShownAdContent = false;
        let evaluateTimer = null;

        function isHidden(element) {
            const style = window.getComputedStyle(element);

            return style.display === 'none' ||
                style.visibility === 'hidden' ||
                style.opacity === '0' ||
                element.hidden ||
                element.getAttribute('aria-hidden') === 'true';
        }

        function hasVisibleAdContent() {
            if (isHidden(adRoot)) {
                return false;
            }

            const candidates = adRoot.querySelectorAll(
                'iframe, img, ins, object, embed, a, [role="img"]'
            );

            return Array.from(candidates).some(function (element) {
                if (isHidden(element)) {
                    return false;
                }

                const rect = element.getBoundingClientRect();

                return rect.width > 2 &&
                    rect.height > 2 &&
                    (
                        element.tagName === 'IFRAME' ||
                        element.tagName === 'IMG' ||
                        element.tagName === 'OBJECT' ||
                        element.tagName === 'EMBED' ||
                        element.textContent.trim().length > 0 ||
                        element.hasAttribute('src') ||
                        element.hasAttribute('href')
                    );
            });
        }

        function setCollapsed(shouldCollapse) {
            shell.classList.toggle('bn-ad-collapsed', shouldCollapse);
            adRoot.toggleAttribute('data-ad-empty', shouldCollapse);
        }

        function setFilled(isFilled) {
            shell.classList.toggle('bn-ad-filled', isFilled);
            adRoot.toggleAttribute('data-ad-filled', isFilled);
        }

        function evaluate(forceCollapseWhenEmpty) {
            const hasContent = hasVisibleAdContent();

            if (hasContent) {
                hasShownAdContent = true;
                setFilled(true);
                setCollapsed(false);
                return;
            }

            setFilled(false);
            setCollapsed(hasShownAdContent || forceCollapseWhenEmpty);
        }

        function scheduleEvaluation(forceCollapseWhenEmpty) {
            window.clearTimeout(evaluateTimer);

            evaluateTimer = window.setTimeout(function () {
                evaluate(forceCollapseWhenEmpty);
            }, 250);
        }

        const observer = new MutationObserver(function () {
            scheduleEvaluation(false);
        });

        observer.observe(adRoot, {
            attributes: true,
            attributeFilter: ['class', 'style', 'hidden', 'aria-hidden'],
            childList: true,
            subtree: true
        });

        window.setTimeout(function () {
            evaluate(false);
        }, 1500);

        window.setTimeout(function () {
            evaluate(true);
        }, 10000);

        window.addEventListener('resize', function () {
            scheduleEvaluation(false);
        }, { passive: true });
    }

    document.querySelectorAll(adSelector).forEach(registerAdRoot);

    const pageObserver =
        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType !== Node.ELEMENT_NODE) {
                        return;
                    }

                    if (node.matches(adSelector)) {
                        registerAdRoot(node);
                    }

                    node.querySelectorAll(adSelector).forEach(registerAdRoot);
                });
            });
        });

    pageObserver.observe(document.body, {
        childList: true,
        subtree: true
    });
}
