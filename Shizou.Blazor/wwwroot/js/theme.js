'use strict';
const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = theme => localStorage.setItem('theme', theme);
const getPreferredTheme = () => {
    const storedTheme = getStoredTheme();
    if (storedTheme) {
        return storedTheme;
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}
const setTheme = theme => {
    if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
    document.querySelector('meta[name="theme-color"]').setAttribute('content', window.getComputedStyle(document.documentElement).getPropertyValue('--bs-secondary-bg'));
}
function getTheme() {
    return document.documentElement.getAttribute("data-bs-theme") || "light";
}
function toggleTheme() {
    const currentTheme = getTheme();
    const theme = currentTheme == "light" ? "dark" : "light";
    setStoredTheme(theme);
    setTheme(theme);
    return theme;
}
setTheme(getPreferredTheme());
