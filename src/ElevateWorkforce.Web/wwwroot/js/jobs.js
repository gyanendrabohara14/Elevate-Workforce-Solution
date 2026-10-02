// ElevateWorkforce — job discovery interactions (filters, save toggling)
(function () {
    "use strict";

    function toggleSaveJob(btn) {
        var jobId = btn.getAttribute("data-job-id");
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var tokenValue = token ? token.value : "";

        fetch("/jobs/togglesave/" + jobId, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: "__RequestVerificationToken=" + encodeURIComponent(tokenValue),
            credentials: "same-origin"
        }).then(function (r) { return r.json(); }).then(function (data) {
            if (data.success) {
                var label = btn.querySelector(".js-save-label");
                if (label) label.textContent = data.saved ? "Saved" : "Save";
                btn.classList.toggle("is-saved", data.saved);
            } else if (data.message) {
                window.location.href = "/account/login";
            }
        }).catch(function () { });
    }

    var saveButtons = document.querySelectorAll("[data-save-job]");
    saveButtons.forEach(function (btn) {
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleSaveJob(btn);
        });
    });

    // Auto-collapse mobile filter drawer
    var filterToggle = document.querySelector(".js-filter-toggle");
    if (filterToggle) {
        var targetId = filterToggle.getAttribute("data-bs-target");
        var collapseEl = document.querySelector(targetId);
        if (collapseEl && window.bootstrap) {
            filterToggle.addEventListener("click", function (e) {
                e.preventDefault();
                bootstrap.Collapse.getOrCreateInstance(collapseEl).toggle();
            });
        }
    }
})();
