// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const eyeIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true"><path d="M16 8s-3-5-8-5-8 5-8 5 3 5 8 5 8-5 8-5zM8 11a3 3 0 1 1 0-6 3 3 0 0 1 0 6z"/><path d="M8 9.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z"/></svg>';
const eyeSlashIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true"><path d="M13.36 10.36C14.94 9.06 16 8 16 8s-3-5-8-5c-1.07 0-2.03.22-2.87.54l.78.78C6.53 3.99 7.23 4 8 4c3.58 0 6.18 2.99 6.98 4-.3.38-1.02 1.18-2.16 1.87l.54.49z"/><path d="M11.83 12.83C10.73 13.55 9.45 14 8 14c-5 0-8-6-8-6s1.06-2.06 2.64-3.36l.7.7C2.31 6.44 1.3 7.55.98 8c.8 1.01 3.4 4 7.02 4 .98 0 1.85-.25 2.67-.64l1.16 1.47z"/><path d="m3.27 2.27.7-.7 9.46 9.46-.7.7z"/></svg>';

document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    const input = document.getElementById(button.dataset.passwordToggle);
    if (!input) return;
    button.innerHTML = eyeSlashIcon;
    button.addEventListener("click", () => {
        const isVisible = input.type === "text";
        input.type = isVisible ? "password" : "text";
        button.innerHTML = isVisible ? eyeSlashIcon : eyeIcon;
        button.setAttribute("aria-label", isVisible ? "Prikaži šifru" : "Sakrij šifru");
        button.setAttribute("aria-pressed", String(!isVisible));
    });
});
