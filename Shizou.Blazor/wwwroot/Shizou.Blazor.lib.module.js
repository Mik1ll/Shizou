import {setTheme} from "/js/theme.js";

// noinspection JSUnusedGlobalSymbols, JSUnusedLocalSymbols
export function beforeServerStart(options, extensions) {
    setTheme();
}
