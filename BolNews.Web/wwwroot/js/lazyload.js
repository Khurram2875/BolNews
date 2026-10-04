document.addEventListener("DOMContentLoaded", function () {

    /*
     * ============================================================
     * BOL NEWS - GLOBAL LAZY IMAGE HANDLER
     * ============================================================
     *
     * Supports custom lazy images:
     * class="lazy-image"
     * data-src="..."
     *
     * ============================================================
     */

    const images = document.querySelectorAll("img.lazy-image[data-src]");

    function createSpinner(img) {
        const parent = img.parentElement;
        if (!parent) return null;

        parent.classList.add("bn-lazy-image-container");

        let spinner = parent.querySelector(".bn-lazy-spinner");
        if (!spinner) {
            spinner = document.createElement("span");
            spinner.className = "bn-lazy-spinner";
            spinner.setAttribute("aria-hidden", "true");
            parent.appendChild(spinner);
        }

        return spinner;
    }

    function imageLoaded(img, spinner) {
        const showImage = function () {
            img.classList.remove("bn-lazy-loading");
            img.classList.add("bn-lazy-loaded");
            if (spinner) spinner.remove();
        };

        // The image is already loaded; do not add an artificial delay.
        setTimeout(showImage, 0);
    }

    function imageFailed(img, spinner) {
        img.classList.add("bn-lazy-loading");
        if (spinner) spinner.style.display = "block";
    }

    function prepareCustomLazyImage(img) {
        if (img.dataset.lazyInitialized === "true") return;

        img.dataset.lazyInitialized = "true";
        img.classList.add("bn-lazy-loading");
        img._bnLazySpinner = createSpinner(img);
    }

    images.forEach(function (img) {
        prepareCustomLazyImage(img);
    });

    const observer = new IntersectionObserver(
        function (entries, observer) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;

                const img = entry.target;

                const src = img.dataset.src;
                const srcset = img.dataset.srcset;

                img.addEventListener("load", function () {
                    imageLoaded(img, img._bnLazySpinner);
                }, { once: true });

                img.addEventListener("error", function () {
                    imageFailed(img, img._bnLazySpinner);
                }, { once: true });

                if (srcset) img.srcset = srcset;
                if (src) img.src = src;

                observer.unobserve(img);
            });
        },
        {
            rootMargin: "250px 0px",
            threshold: 0.01
        }
    );

    images.forEach(function (img) {
        observer.observe(img);
    });
});
