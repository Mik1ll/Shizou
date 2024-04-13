'use strict';

function getStoredTheme() {
    return localStorage.getItem('shizou.theme') || 'auto';
}

function setStoredTheme(theme) {
    return localStorage.setItem('shizou.theme', theme);
}

function setTheme(theme) {
    setStoredTheme(theme);
    if (theme === 'auto') {
        document.documentElement.setAttribute('data-bs-theme', window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
    let meta = document.querySelector('meta[name="theme-color"]');
    if (meta != null) {
        meta.setAttribute('content', window.getComputedStyle(document.documentElement).getPropertyValue('--bs-secondary-bg'));
    }
}

// noinspection JSUnusedGlobalSymbols
function cycleTheme() {
    const themes = ['auto', 'light', 'dark'];
    let currThemeIdx = themes.indexOf(getStoredTheme());
    if (currThemeIdx < 0) currThemeIdx = 0;
    const themeIdx = ++currThemeIdx % themes.length;
    setTheme(themes[themeIdx]);
    return themes[themeIdx];
}

setTheme(getStoredTheme());
