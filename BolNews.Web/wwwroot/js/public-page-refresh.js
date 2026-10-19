(() => {
    'use strict';

    const refreshAfterMs = 15 * 60 * 1000;
    window.setTimeout(() => {
        window.location.reload();
    }, refreshAfterMs);
})();
