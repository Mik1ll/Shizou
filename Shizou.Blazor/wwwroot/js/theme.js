function getPreferredTheme() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function getThemeColor() {
    return window.getComputedStyle(document.documentElement).getPropertyValue('--bs-secondary-bg');
}
