(function () {
    'use strict';

    function initLivePlayer() {
        var thumbnail = document.getElementById('gy-live-thumb');
        var player = document.getElementById('gy-live-player');
        var iframe = document.getElementById('gy-live-iframe');

        if (!thumbnail || !player || !iframe) return;
        var embedUrl = thumbnail.dataset.liveEmbedUrl;
        if (!embedUrl) return;

        thumbnail.addEventListener('click', function () {
            thumbnail.style.display = 'none';
            iframe.setAttribute(
                'src',
                embedUrl + (embedUrl.indexOf('?') >= 0 ? '&' : '?') + 'autoplay=1'
            );
            player.style.display = 'block';
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initLivePlayer, { once: true });
    } else {
        initLivePlayer();
    }
})();
