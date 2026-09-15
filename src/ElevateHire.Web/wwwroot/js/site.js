// ElevateWorkforce — site interactions & motion

(function () {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Header shadow on scroll
    var header = document.querySelector(".js-header");
    function onScroll() {
        if (!header) return;
        header.style.boxShadow = window.scrollY > 10 ? "0 2px 12px rgba(9,9,11,.06)" : "none";
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    // GSAP entrance animations (only when available and motion is allowed)
    if (!reduceMotion && window.gsap) {
        gsap.registerPlugin(ScrollTrigger);

        // Hero reveal
        var heroHeading = document.querySelector(".hero__heading");
        if (heroHeading) {
            var heroItems = document.querySelectorAll(".hero .hero-reveal");
            gsap.from(heroItems, {
                y: 30, opacity: 0, duration: 0.7, stagger: 0.12, ease: "power3.out",
                delay: 0.1
            });
        }

        // Floating hero cards
        var heroCards = document.querySelectorAll(".hero__card");
        if (heroCards.length) {
            gsap.from(heroCards, {
                y: 40, opacity: 0, duration: 0.8, stagger: 0.15, delay: 0.3, ease: "power3.out"
            });
            heroCards.forEach(function (card) {
                gsap.to(card, {
                    y: -12, duration: 3.2, yoyo: true, repeat: -1, ease: "sine.inOut", delay: 1
                });
            });
        }

        // Scroll reveals
        gsap.utils.toArray(".reveal").forEach(function (el) {
            gsap.fromTo(el, { y: 36, opacity: 0 }, {
                y: 0, opacity: 1, duration: 0.7, ease: "power2.out",
                scrollTrigger: { trigger: el, start: "top 88%" }
            });
        });

        // Counters
        gsap.utils.toArray("[data-count]").forEach(function (el) {
            var target = parseFloat(el.getAttribute("data-count"));
            var obj = { val: 0 };
            gsap.to(obj, {
                val: target, duration: 1.4, ease: "power2.out",
                scrollTrigger: { trigger: el, start: "top 90%" },
                onUpdate: function () {
                    if (isNaN(target)) return;
                    el.textContent = target % 1 === 0
                        ? Math.round(obj.val).toLocaleString()
                        : obj.val.toFixed(1);
                }
            });
        });
    }

    // Smooth anchor scroll
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener("click", function (e) {
            var target = document.querySelector(this.getAttribute("href"));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: reduceMotion ? "auto" : "smooth", block: "start" });
            }
        });
    });
})();

// Account-type role tabs on the login page
(function () {
    var tabs = document.querySelectorAll(".login-roles .login-role");
    var field = document.querySelector("#loginForm input[name='Role']");
    if (!tabs.length || !field) return;

    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            tabs.forEach(function (t) { t.classList.remove("active"); });
            tab.classList.add("active");
            field.value = tab.getAttribute("data-role");
        });
    });
})();

// Save job toggle
function toggleSaveJob(btn, jobId) {
    var token = btn.closest("form") ? btn.closest("form").querySelector("input[name='__RequestVerificationToken']") : null;
    var form = document.createElement("form");
    form.method = "post";
    form.action = "/jobs/togglesave/" + jobId;
    var t = document.createElement("input");
    t.type = "hidden";
    t.name = "__RequestVerificationToken";
    t.value = token ? token.value : "";
    form.appendChild(t);
    document.body.appendChild(form);
    fetch(btn.closest("form") ? null : "/jobs/togglesave", { method: "POST" }); // no-op fallback

    fetch("/jobs/togglesave/" + jobId, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: "__RequestVerificationToken=" + encodeURIComponent(form.querySelector("input").value),
        credentials: "same-origin"
    }).then(function (r) { return r.json(); }).then(function (data) {
        form.remove();
        if (data.success) {
            btn.classList.toggle("is-saved", data.saved);
            btn.setAttribute("aria-pressed", data.saved ? "true" : "false");
            if (btn.querySelector(".js-save-label")) {
                btn.querySelector(".js-save-label").textContent = data.saved ? "Saved" : "Save";
            }
        }
    }).catch(function () { form.remove(); });
}