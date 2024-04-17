"use strict";

// noinspection ES6ConvertVarToLetConst
var subtitleHandler = {
    instance: null,
    dispose: function () {
        if (subtitleHandler.instance) {
            subtitleHandler.instance.dispose();
            subtitleHandler.instance = null;
        }
    },
    free: function () {
        if (subtitleHandler.instance) {
            subtitleHandler.instance.freeTrack();
            subtitleHandler.instance.setSubUrl('');
        }
    },
    setTrack: function (subUrl) {
        if (subtitleHandler.instance && subUrl !== subtitleHandler.instance.subUrl) {
            subtitleHandler.free();
            subtitleHandler.instance.setTrackByUrl(subUrl);
            subtitleHandler.instance.setSubUrl(subUrl);
        }
    }
};

// noinspection JSUnusedGlobalSymbols
function loadPlayer(elementId, subtitleUrls, fontUrls) {
    videojs(document.getElementById(elementId), {fluid: true}, function onPlayerReady() {
        if (subtitleUrls.length <= 0) {
            return;
        }
        const video = this.tech_.el_;
        const options = {
            video: video, // HTML5 video element
            subUrl: subtitleUrls[0], // Link to subtitles
            fonts: fontUrls,
            workerUrl: '/lib/libass-wasm/js/subtitles-octopus-worker.min.js', // Link to WebAssembly-based file "libassjs-worker.js"
            legacyWorkerUrl: '/lib/libass-wasm/js/subtitles-octopus-worker-legacy.min.js', // Link to non-WebAssembly worker
            fallbackFont: '/fonts/default.woff2'
        };

        subtitleHandler.dispose()
        subtitleHandler.instance = new SubtitlesOctopus(options);
        this.textTracks().addEventListener('change', (event) => {
            const tracks = this.textTracks().tracks_;
            const showingIdx = tracks.findIndex((track) => track.mode === 'showing');
            for (let i = 0; i < tracks.length; i++) {
                if (i !== showingIdx) {
                    tracks[i].mode = 'disabled';
                }
            }
            if (showingIdx >= 0) {
                subtitleHandler.setTrack(subtitleUrls[showingIdx]);
            } else {
                subtitleHandler.free();
            }
        });
    });
}
