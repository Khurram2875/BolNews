document.addEventListener("DOMContentLoaded", function () {
    const images = document.querySelectorAll(".lazy-image");

    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;

                img.src = img.dataset.src;

                if (img.dataset.srcset) {
                    img.srcset = img.dataset.srcset;
                }

                img.classList.add("loaded");
                observer.unobserve(img);
            }
        });
    });

    images.forEach(img => observer.observe(img));
});