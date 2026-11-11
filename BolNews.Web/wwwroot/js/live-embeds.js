/*
 * Restores social embeds inside Live Event rich text (intro box and updates) on the public page.
 * Mirrors the article page: each div.quill-social-embed carries its original embed code, base64 encoded,
 * in data-raw-html. Scripts inside are never inserted directly; their external src is loaded once.
 */
(function () {
    'use strict';
    document.addEventListener('DOMContentLoaded', function () {
        const embeds = document.querySelectorAll('.bn-live-rich .quill-social-embed');
        if (!embeds.length) return;
        const scriptSrcs = new Set();

        embeds.forEach(function (container) {
            const data = container.getAttribute('data-raw-html');
            if (!data) return;
            try {
                const tmp = document.createElement('div');
                tmp.innerHTML = decodeURIComponent(escape(atob(data)));
                tmp.querySelectorAll('script').forEach(function (s) {
                    if (s.src && !s.src.includes('youtube.com') && !s.src.includes('google.com')) scriptSrcs.add(s.src);
                    s.remove();
                });
                // Embed codes never need inline handlers or javascript: links.
                tmp.querySelectorAll('*').forEach(function (el) {
                    Array.from(el.attributes).forEach(function (a) {
                        if (/^on/i.test(a.name) || (/^(href|src|action|formaction)$/i.test(a.name) && /^\s*javascript:/i.test(a.value))) el.removeAttribute(a.name);
                    });
                });
                container.innerHTML = tmp.innerHTML;
            } catch (e) { console.error('Could not restore embed', e); }
        });

        window.twttr = (function (d, s, id) {
            const fjs = d.getElementsByTagName(s)[0], t = window.twttr || {};
            if (d.getElementById(id)) return t;
            const js = d.createElement(s); js.id = id; js.src = 'https://platform.twitter.com/widgets.js';
            fjs.parentNode.insertBefore(js, fjs);
            t._e = []; t.ready = function (f) { t._e.push(f); };
            return t;
        }(document, 'script', 'twitter-wjs'));

        scriptSrcs.forEach(function (src) {
            if (src.includes('twitter.com') || src.includes('x.com')) return;
            if (document.querySelector('script[src="' + src + '"]')) return;
            const node = document.createElement('script');
            node.src = src; node.async = true; node.charset = 'utf-8';
            document.head.appendChild(node);
        });

        setTimeout(function () {
            if (window.twttr && typeof window.twttr.ready === 'function') {
                window.twttr.ready(function (tw) {
                    embeds.forEach(function (container) {
                        const a = container.querySelector('.twitter-tweet a[href*="/status/"]');
                        const m = a && a.getAttribute('href').match(/\/status\/(\d+)/);
                        if (m && m[1]) {
                            container.innerHTML = '';
                            tw.widgets.createTweet(m[1], container, { theme: 'light', align: 'center' }).catch(function () { });
                        }
                    });
                });
            }
            embeds.forEach(function (container) {
                if (window.FB) window.FB.XFBML.parse(container);
                if (window.instgrm && window.instgrm.Embeds) window.instgrm.Embeds.process();
                if (window.tiktok && window.tiktok.embed && container.querySelector('.tiktok-embed')) window.tiktok.embed.render(container);
            });
        }, 150);
    });
})();
