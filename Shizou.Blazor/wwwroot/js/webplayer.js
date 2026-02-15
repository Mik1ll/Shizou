"use strict";

import JASSUB from './jassub_dist/jassub.js';

class WebPlayer {
    /** @type {HTMLElement} */
    video;
    /** @type {string[]} */
    subUrls;
    /** @type {string[]} */
    fontUrls;
    /** @type {Player} */
    player;
    /** @type {JASSUB | null} */
    jassub;

    /**
     * @param {string} elementId
     * @param {string[]} subtitleUrls
     * @param {string[]} fontUrls
     */
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
            const subUrl = new URL(this.subUrls[this.activeSub], import.meta.url).href;
            this.jassub = new JASSUB({
                video: this.video, // HTML5 video element
                subUrl: subUrl, // Link to subtitles
            });
            this.jassub.ready.then(() => {
                this.player.textTracks().addEventListener('change', () => {
                    const tracks = this.player.textTracks().tracks_;
                    const showingIdx = tracks.findIndex(track => track.mode === 'showing');
                    if (showingIdx >= 0) {
                        if (showingIdx !== this.activeSub) {
                            this.jassub?.renderer.setTrackByUrl(this.subUrls[showingIdx]);
                            this.activeSub = showingIdx;
                        }
                    } else {
                        this.jassub?.renderer.freeTrack();
                        this.activeSub = -1;
                    }
                });
            });
        }
    }

    dispose() {
        this.jassub?.destroy();
        this.player?.dispose();
    }
}

// noinspection JSUnusedGlobalSymbols
export function newPlayer(elementId, subtitleUrls, fontUrls) {
    return new WebPlayer(elementId, subtitleUrls, fontUrls);
}
