// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
window.onscroll = function () {
    const nav = document.getElementById("sticky-navbar");
    const body = document.body;
    const stickyTrigger = 150; // The point where logo/headlines are fully scrolled past

    if (window.pageYOffset > stickyTrigger) {
        nav.classList.add("fixed-nav");
        body.classList.add("nav-fixed");
    } else {
        nav.classList.remove("fixed-nav");
        body.classList.remove("nav-fixed");
    }
};