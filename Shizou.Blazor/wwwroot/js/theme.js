'use strict';
const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = theme => localStorage.setItem('theme', theme);
const getPreferredTheme = () => {
    const storedTheme = getStoredTheme();
    if (storedTheme) {
        return storedTheme
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}
const setTheme = theme => {
    if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.setAttribute('data-bs-theme', 'dark')
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme)
    }
}
window.toggleTheme = () => {
    const root = document.documentElement;
    const currentTheme = root.getAttribute("data-bs-theme") || "light";
    const theme = currentTheme == "light" ? "dark" : "light";
    setStoredTheme(theme);
    setTheme(theme);
}
setTheme(getPreferredTheme())