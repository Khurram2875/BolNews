document.addEventListener("DOMContentLoaded", function () {

    /*
     * ============================================================
     * BOL NEWS - GLOBAL LAZY IMAGE HANDLER
     * ============================================================
     *
     * Supports:
     *
     * 1. Custom lazy images
     *    class="lazy-image"
     *    data-src="..."
     *
     * 2. Native lazy images
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


        parent.classList.add(
            "bn-lazy-image-container"
        );


        let spinner = parent.querySelector(
            ".bn-lazy-spinner"
        );


        if (!spinner) {

            spinner = document.createElement("span");

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

        /*
         * Keep image hidden initially.
         */
        const showImage = function () {

            img.classList.remove(
                "bn-lazy-loading"
            );

            img.classList.add(
                "bn-lazy-loaded"
            );

            if (spinner) {
                spinner.remove();
            }

        };


        /*
         * Give the spinner enough time to be visible.
         */
        setTimeout(
            showImage,
            300
        );

    }

    /*
     * ------------------------------------------------------------
     * Image failed
     * ------------------------------------------------------------
     */
    function imageFailed(img, spinner) {

        /*
         * Keep the image hidden.
         *
         * This prevents the browser from showing:
         * broken image icon + ALT text.
         */
        img.classList.add(
            "bn-lazy-loading"
        );


        /*
         * Keep spinner visible.
         */
        if (spinner) {

            spinner.style.display =
                "block";
        }

    }


    /*
     * ------------------------------------------------------------
     * Prepare custom lazy image
     * ------------------------------------------------------------
     *
     * IMPORTANT:
     *
     * Do NOT check img.complete here.
     *
     * The current src is normally the placeholder image.
     * The placeholder may already be loaded, but the real
     * data-src image has NOT been loaded yet.
     * ------------------------------------------------------------
     */
    function prepareCustomLazyImage(img) {

        if (
            img.dataset.lazyInitialized === "true"
        ) {
            return;
        }


        img.dataset.lazyInitialized =
            "true";


        /*
         * Hide the placeholder.
         */
        img.classList.add(
            "bn-lazy-loading"
        );


        /*
         * Show spinner.
         */
        const spinner =
            createSpinner(img);


        /*
         * Store spinner reference.
         */
        img._bnLazySpinner =
            spinner;
    }


    /*
     * ------------------------------------------------------------
     * Prepare native lazy image
     * ------------------------------------------------------------
     */
    function prepareNativeLazyImage(img) {

        if (
            img.dataset.lazyInitialized === "true"
        ) {
            return;
        }


        img.dataset.lazyInitialized =
            "true";


        /*
         * Hide actual image while it loads.
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
         * Native lazy image may already have
         * been downloaded by the browser.
         */
        if (
            img.complete &&
            img.naturalWidth > 0
        ) {

            imageLoaded(
                img,
                spinner
            );
        }

    }


    /*
     * ------------------------------------------------------------
     * Prepare images
     * ------------------------------------------------------------
     */
    images.forEach(function (img) {

        /*
         * Custom lazy image
         */
        if (
            img.classList.contains(
                "lazy-image"
            ) &&
            img.dataset.src
        ) {

            prepareCustomLazyImage(
                img
            );

        }
        else {

            /*
             * Native loading="lazy"
             */
            prepareNativeLazyImage(
                img
            );

        }

    });


    /*
     * ------------------------------------------------------------
     * Intersection Observer
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
                     * Only custom lazy images
                     * need IntersectionObserver
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
                         * Attach load/error handlers
                         * BEFORE assigning src.
                         *
                         * This is important.
                         */
                        img.addEventListener(
                            "load",
                            function () {

                                imageLoaded(
                                    img,
                                    img._bnLazySpinner
                                );

                            },
                            { once: true }
                        );


                        img.addEventListener(
                            "error",
                            function () {

                                imageFailed(
                                    img,
                                    img._bnLazySpinner
                                );

                            },
                            { once: true }
                        );


                        /*
                         * Set srcset first.
                         */
                        if (srcset) {

                            img.srcset =
                                srcset;
                        }


                        /*
                         * Set the REAL image source.
                         */
                        if (src) {

                            img.src =
                                src;
                        }


                        /*
                         * Stop observing.
                         */
                        observer.unobserve(
                            img
                        );

                    }

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
     * ------------------------------------------------------------
     * Observe custom lazy images
     * ------------------------------------------------------------
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