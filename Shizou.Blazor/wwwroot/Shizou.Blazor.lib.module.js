// noinspection JSUnusedGlobalSymbols,JSUnusedLocalSymbols

'use strict';

import {setTheme} from "./js/theme.js";

export function beforeWebStart(options, extensions) {
    import("/lib/video.js/alt/video.novtt.min.js");
    setTheme();
}

export function afterWebStarted(blazor) {
    blazor.addEventListener("enhancedload", function () {
        setTheme();
    });
}
