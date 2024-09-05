'use strict';

export function getStoredTheme() {
    return localStorage.getItem('shizou.theme') || 'auto';
}

export function setStoredTheme(theme) {
    return localStorage.setItem('shizou.theme', theme);
}

export function setTheme(theme) {
    theme = theme || getStoredTheme();
    setStoredTheme(theme);
    if (theme === 'auto') {
        document.documentElement.setAttribute('data-bs-theme', prefersDark() ? 'dark' : 'light');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
    let meta = document.querySelector('meta[name="theme-color"]');
    if (meta != null) {
        meta.setAttribute('content', getBgColor());
    }
    document.body.classList.remove('invisible');
}

// noinspection JSUnusedGlobalSymbols
export function cycleTheme() {
    const themes = ['auto', 'light', 'dark'];
    let currThemeIdx = themes.indexOf(getStoredTheme());
    if (currThemeIdx < 0) currThemeIdx = 0;
    const themeIdx = ++currThemeIdx % themes.length;
    setTheme(themes[themeIdx]);
    return themes[themeIdx];
}

export function getBgColor() {
    return window.getComputedStyle(document.documentElement).getPropertyValue('--bs-secondary-bg');
}

export function prefersDark() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}
