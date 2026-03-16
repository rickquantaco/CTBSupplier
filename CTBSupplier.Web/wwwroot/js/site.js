// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Wrap every required input/select in a flex container so the red asterisk
// appears immediately to the right on the same line.
// ASP.NET MVC tag helpers emit data-val-required on all NOT NULL fields.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-val-required]').forEach(function (el) {
        // Skip checkboxes and hidden inputs
        if (el.type === 'checkbox' || el.type === 'hidden') return;

        var wrapper = document.createElement('div');
        wrapper.className = 'required-field-wrapper';

        el.parentNode.insertBefore(wrapper, el);
        wrapper.appendChild(el);

        var asterisk = document.createElement('span');
        asterisk.textContent = '*';
        asterisk.className = 'required-asterisk';
        asterisk.setAttribute('aria-hidden', 'true');
        wrapper.appendChild(asterisk);
    });
});
