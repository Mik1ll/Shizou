"use strict";

import JASSUB from '../lib/jassub/dist/jassub.es.js';

class WebPlayer {
    constructor(elementId, subtitleUrls, fontUrls) {
        this.video = document.getElementById(elementId);
        this.subUrls = subtitleUrls;
        this.fontUrls = fontUrls;
        this.player = videojs(this.video, {
            fluid: true,
            textTrackSettings: false,
            html5: {
                nativeTextTracks: true
            }
        });
        this.player.ready(() => this.onPlayerReady());
    }

    onPlayerReady() {
        if (this.subUrls.length > 0) {
            this.activeSub = 0;
            this.player.textTracks()[this.activeSub].mode = 'showing';
            this.subRenderer = new JASSUB({
                video: this.video, // HTML5 video element
                subUrl: this.subUrls[this.activeSub], // Link to subtitles
                fonts: this.fontUrls,
                workerUrl: new URL('/lib/jassub/dist/jassub-worker.js', import.meta.url).toString(),
                wasmUrl: new URL('/lib/jassub/dist/jassub-worker.wasm', import.meta.url).toString(),
                legacyWasmUrl: new URL('/lib/jassub/dist/jassub-worker.wasm.js', import.meta.url).toString(),
                modernWasmUrl: new URL('/lib/jassub/dist/jassub-worker-modern.wasm', import.meta.url).toString(),
                availableFonts: {'liberation sans': '/lib/jassub/dist/default.woff2'},
                fallbackFont: 'liberation sans',
            });
        }
        this.player.textTracks().addEventListener('change', () => {
            const tracks = this.player.textTracks().tracks_;
            const showingIdx = tracks.findIndex(track => track.mode === 'showing');
            if (showingIdx >= 0) {
                if (showingIdx !== this.activeSub) {
                    this.subRenderer?.setTrackByUrl(this.subUrls[showingIdx]);
                    this.activeSub = showingIdx;
                }
            } else {
                this.subRenderer?.freeTrack();
                this.activeSub = -1;
            }
        });
    }

    dispose() {
        this.subRenderer?.destroy();
        this.player?.dispose();
    }
}

// noinspection JSUnusedGlobalSymbols
export function newPlayer(elementId, subtitleUrls, fontUrls) {
    return new WebPlayer(elementId, subtitleUrls, fontUrls);
}
