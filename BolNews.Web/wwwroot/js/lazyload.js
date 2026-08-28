//Original code
//document.addEventListener("DOMContentLoaded", function () {
//    const images = document.querySelectorAll(".lazy-image");

//    const observer = new IntersectionObserver((entries, observer) => {
//        entries.forEach(entry => {
//            if (entry.isIntersecting) {
//                const img = entry.target;

//                img.src = img.dataset.src;

//                if (img.dataset.srcset) {
//                    img.srcset = img.dataset.srcset;
//                }

//                img.classList.add("loaded");
//                observer.unobserve(img);
//            }
//        });
//    });

//    images.forEach(img => observer.observe(img));
//});
//Original code
document.addEventListener("DOMContentLoaded", function () {

    /*
     * ============================================================
     * BOL NEWS - GLOBAL LAZY IMAGE HANDLER
     * ============================================================
     *
     * Supports:
     *
     * 1. Custom lazy images
     *
     *    class="lazy-image"
     *    data-src="..."
     *
     * 2. Native lazy images
     *
     *    loading="lazy"
     *
     * ============================================================
     */


    const images = document.querySelectorAll(
        'img.lazy-image, img[loading="lazy"]'
    );


    /*
     * ------------------------------------------------------------
     * Create spinner
     * ------------------------------------------------------------
     */
    function createSpinner(img) {

        const parent = img.parentElement;

        if (!parent) {
            return null;
        }


        /*
         * Make parent the spinner positioning container.
         */
        parent.classList.add(
            "bn-lazy-image-container"
        );


        /*
         * Don't create duplicate spinners.
         */
        let spinner =
            parent.querySelector(
                ".bn-lazy-spinner"
            );

        if (!spinner) {

            spinner =
                document.createElement("span");

            spinner.className =
                "bn-lazy-spinner";

            spinner.setAttribute(
                "aria-hidden",
                "true"
            );

            parent.appendChild(
                spinner
            );
        }


        return spinner;
    }


    /*
     * ------------------------------------------------------------
     * Image successfully loaded
     * ------------------------------------------------------------
     */
    function imageLoaded(img, spinner) {

        img.classList.remove(
            "bn-lazy-loading"
        );

        img.classList.add(
            "bn-lazy-loaded"
        );


        if (spinner) {
            spinner.remove();
        }

    }


    /*
     * ------------------------------------------------------------
     * Image failed
     * ------------------------------------------------------------
     */
    function imageFailed(img, spinner) {

        img.classList.remove(
            "bn-lazy-loading"
        );


        /*
         * Hide the broken image and its ALT fallback.
         */
        img.classList.add(
            "bn-lazy-loading"
        );


        /*
         * Keep the spinner visible.
         *
         * This prevents the ugly broken-image/ALT-text
         * appearance visible in your screenshot.
         */
        if (spinner) {

            spinner.style.display =
                "block";

        }

    }


    /*
     * ------------------------------------------------------------
     * Prepare image
     * ------------------------------------------------------------
     */
    function prepareImage(img) {

        /*
         * Prevent duplicate initialization.
         */
        if (
            img.dataset.lazyInitialized === "true"
        ) {
            return;
        }


        img.dataset.lazyInitialized =
            "true";


        /*
         * Hide image immediately.
         *
         * This happens before we start loading
         * the actual image.
         */
        img.classList.add(
            "bn-lazy-loading"
        );


        /*
         * Create spinner.
         */
        const spinner =
            createSpinner(img);


        /*
         * Successful load.
         */
        img.addEventListener(
            "load",
            function () {

                imageLoaded(
                    img,
                    spinner
                );

            },
            { once: true }
        );


        /*
         * Failed load.
         */
        img.addEventListener(
            "error",
            function () {

                imageFailed(
                    img,
                    spinner
                );

            }
        );


        /*
         * If browser already loaded the image.
         */
        if (img.complete) {

            if (
                img.naturalWidth > 0
            ) {

                imageLoaded(
                    img,
                    spinner
                );

            }

        }

    }


    /*
     * ------------------------------------------------------------
     * Prepare all images immediately
     * ------------------------------------------------------------
     */
    images.forEach(function (img) {

        prepareImage(img);

    });


    /*
     * ------------------------------------------------------------
     * Intersection Observer
     * ------------------------------------------------------------
     *
     * Only custom .lazy-image images need us to assign
     * data-src when they enter the viewport.
     *
     * Native loading="lazy" images are handled by the
     * browser itself.
     * ------------------------------------------------------------
     */

    const observer =
        new IntersectionObserver(
            function (entries, observer) {

                entries.forEach(function (entry) {

                    if (!entry.isIntersecting) {
                        return;
                    }


                    const img =
                        entry.target;


                    /*
                     * Custom lazy-image
                     */
                    if (
                        img.classList.contains(
                            "lazy-image"
                        )
                    ) {

                        const src =
                            img.dataset.src;

                        const srcset =
                            img.dataset.srcset;


                        /*
                         * Set srcset first.
                         */
                        if (srcset) {

                            img.srcset =
                                srcset;

                        }


                        /*
                         * Set actual source.
                         */
                        if (src) {

                            img.src =
                                src;

                        }

                    }


                    /*
                     * Stop observing this image.
                     */
                    observer.unobserve(
                        img
                    );

                });

            },
            {
                rootMargin:
                    "250px 0px",

                threshold:
                    0.01
            }
        );


    /*
     * Observe ONLY custom lazy images.
     *
     * Native loading="lazy" is controlled by browser.
     */
    images.forEach(function (img) {

        if (
            img.classList.contains(
                "lazy-image"
            ) &&
            img.dataset.src
        ) {

            observer.observe(
                img
            );

        }

    });

});