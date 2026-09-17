(function () {
    'use strict';

    function initLivePlayer() {
        var thumbnail = document.getElementById('gy-live-thumb');
        var player = document.getElementById('gy-live-player');
        var iframe = document.getElementById('gy-live-iframe');

        if (!thumbnail || !player || !iframe) return;

        thumbnail.addEventListener('click', function () {
            thumbnail.style.display = 'none';
            iframe.setAttribute(
                'src',
                'https://www.youtube.com/embed/9N6SWn1BQpA?si=OenP3TYNCWNSIv57&autoplay=1&rel=0&modestbranding=1'
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
