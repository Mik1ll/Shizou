import {
  proxy,
  releaseProxy,
  wrap
} from "./chunk-LNQWGF3A.js";
import {
  __commonJS,
  __toESM
} from "./chunk-5WRI5ZAA.js";

// node_modules/throughput/index.js
var require_throughput = __commonJS({
  "node_modules/throughput/index.js"(exports, module) {
    var hrtime = typeof process !== "undefined" && !!process.hrtime;
    var maxTick = 65535;
    var resolution = 10;
    var timeDiff = hrtime ? 1e9 / resolution : 1e3 / resolution;
    var now = hrtime ? () => {
      const [seconds, nanoseconds] = process.hrtime();
      return seconds * 1e9 + nanoseconds;
    } : () => performance.now();
    function getTick(start) {
      return (now() - start) / timeDiff & maxTick;
    }
    module.exports = function(seconds) {
      const start = now();
      const size = resolution * (seconds || 5);
      const buffer = [0];
      let pointer = 1;
      let last = getTick(start) - 1 & maxTick;
      return function(delta) {
        const tick = getTick(start);
        let dist = tick - last & maxTick;
        if (dist > size) dist = size;
        last = tick;
        while (dist--) {
          if (pointer === size) pointer = 0;
          buffer[pointer] = buffer[pointer === 0 ? size - 1 : pointer - 1];
          pointer++;
        }
        if (delta) buffer[pointer - 1] += delta;
        const top = buffer[pointer - 1];
        const btm = buffer.length < size ? 0 : buffer[pointer === size ? 0 : pointer];
        return buffer.length < resolution ? top : (top - btm) * resolution / buffer.length;
      };
    };
  }
});

// node_modules/rvfc-polyfill/index.js
var VidProto = typeof HTMLVideoElement !== "undefined" ? HTMLVideoElement.prototype : {};
var hasQuality = "getVideoPlaybackQuality" in VidProto || "webkitDecodedFrameCount" in VidProto || "mozPresentedFrames" in VidProto || "mozPaintedFrames" in VidProto;
if (!("requestVideoFrameCallback" in VidProto) && hasQuality && typeof requestAnimationFrame === "function") {
  VidProto._rvfcpolyfillmap = {};
  const getPlaybackQuality = "getVideoPlaybackQuality" in VidProto ? (video) => {
    const { totalFrameDelay, totalVideoFrames, droppedVideoFrames } = video.getVideoPlaybackQuality();
    return {
      presentedFrames: totalVideoFrames - droppedVideoFrames,
      totalFrameDelay
    };
  } : (video) => {
    return {
      presentedFrames: video.mozPresentedFrames || video.mozPaintedFrames || video.webkitDecodedFrameCount - (video.webkitDroppedFrameCount || 0),
      totalFrameDelay: video.mozFrameDelay || 0
    };
  };
  VidProto.requestVideoFrameCallback = function(callback) {
    const handle = performance.now();
    const quality = getPlaybackQuality(this);
    const baseline = quality.presentedFrames;
    const check = (old, now) => {
      const newquality = getPlaybackQuality(this);
      const presentedFrames = newquality.presentedFrames;
      if (presentedFrames > baseline) {
        const processingDuration = newquality.totalFrameDelay - quality.totalFrameDelay || 0;
        const timediff = now - old;
        callback(now, {
          presentationTime: now + processingDuration * 1e3,
          expectedDisplayTime: now + timediff,
          width: this.videoWidth,
          height: this.videoHeight,
          mediaTime: Math.max(0, this.currentTime || 0) + timediff / 1e3,
          presentedFrames,
          processingDuration
        });
        delete this._rvfcpolyfillmap[handle];
      } else {
        this._rvfcpolyfillmap[handle] = requestAnimationFrame((newer) => check(now, newer));
      }
    };
    this._rvfcpolyfillmap[handle] = requestAnimationFrame((newer) => check(handle, newer));
    return handle;
  };
  VidProto.cancelVideoFrameCallback = function(handle) {
    cancelAnimationFrame(this._rvfcpolyfillmap[handle]);
    delete this._rvfcpolyfillmap[handle];
  };
}

// node_modules/jassub/dist/debug.js
var import_throughput = __toESM(require_throughput(), 1);
var Debug = class {
  // 5 second average
  fps = (0, import_throughput.default)(5);
  processingDuration = (0, import_throughput.default)(5);
  droppedFrames = 0;
  presentedFrames = 0;
  mistimedFrames = 0;
  _drop() {
    ++this.droppedFrames;
  }
  _startTime = 0;
  _startFrame() {
    this._startTime = performance.now();
  }
  onsubtitleFrameCallback = (_, { fps, processingDuration, droppedFrames }) => console.info("%cFPS: %c%f %c| Frame Time: %c%d ms %c| Dropped Frames: %c%d %c| 5s Avg", "color: #888", "color: #0f0; font-weight: bold", fps.toFixed(1), "color: #888", "color: #0ff; font-weight: bold", processingDuration, "color: #888", "color: #f00; font-weight: bold", droppedFrames, "color: #888");
  _endFrame(meta) {
    ++this.presentedFrames;
    const fps = this.fps(1);
    const now = performance.now();
    const processingDuration = this.processingDuration((now - this._startTime) / fps);
    const frameDelay = Math.max(0, now - meta.expectedDisplayTime);
    if (frameDelay)
      ++this.mistimedFrames;
    this.onsubtitleFrameCallback?.(now, {
      fps,
      processingDuration,
      droppedFrames: this.droppedFrames,
      presentedFrames: this.presentedFrames,
      mistimedFrames: this.mistimedFrames,
      presentationTime: now,
      expectedDisplayTime: meta.expectedDisplayTime + (frameDelay > 0 ? fps / 1e3 : 0),
      frameDelay,
      width: meta.width,
      height: meta.height,
      mediaTime: meta.mediaTime
    });
  }
};

// node_modules/jassub/dist/jassub.js
var webYCbCrMap = {
  rgb: "RGB",
  bt709: "BT709",
  // these might not be exactly correct? oops?
  bt470bg: "BT601",
  // alias BT.601 PAL... whats the difference?
  smpte170m: "BT601"
  // alias BT.601 NTSC... whats the difference?
};
var JASSUB = class _JASSUB {
  timeOffset;
  prescaleFactor;
  prescaleHeightLimit;
  maxRenderHeight;
  debug;
  renderer;
  ready;
  busy = false;
  _video;
  _videoWidth = 0;
  _videoHeight = 0;
  _videoColorSpace = null;
  _canvas;
  _canvasParent;
  _ctrl = new AbortController();
  _ro = new ResizeObserver(async () => {
    await this.ready;
    this.resize();
  });
  _destroyed = false;
  _lastDemandTime;
  _skipped = false;
  _worker;
  constructor(opts) {
    if (!globalThis.Worker)
      throw new Error("Worker not supported");
    if (!opts)
      throw new Error("No options provided");
    if (!opts.video && !opts.canvas)
      throw new Error("You should give video or canvas in options.");
    _JASSUB._test();
    this.timeOffset = opts.timeOffset ?? 0;
    this._video = opts.video;
    this._canvas = opts.canvas ?? document.createElement("canvas");
    if (this._video && !opts.canvas) {
      this._canvasParent = document.createElement("div");
      this._canvasParent.className = "JASSUB";
      this._canvasParent.style.position = "relative";
      this._canvas.style.display = "block";
      this._canvas.style.position = "absolute";
      this._canvas.style.pointerEvents = "none";
      this._canvasParent.appendChild(this._canvas);
      this._video.insertAdjacentElement("afterend", this._canvasParent);
    }
    const ctrl = this._canvas.transferControlToOffscreen();
    this.debug = opts.debug ? new Debug() : null;
    this.prescaleFactor = opts.prescaleFactor ?? 1;
    this.prescaleHeightLimit = opts.prescaleHeightLimit ?? 1080;
    this.maxRenderHeight = opts.maxRenderHeight ?? 0;
    this._worker = opts.workerUrl ? new Worker(opts.workerUrl, { name: "jassub-worker", type: "module" }) : new Worker(new URL("./worker/worker.js", import.meta.url), { name: "jassub-worker", type: "module" });
    const Renderer = wrap(this._worker);
    const modern = opts.modernWasmUrl ?? new URL("./wasm/jassub-worker-modern.wasm", import.meta.url).href;
    const normal = opts.wasmUrl ?? new URL("./wasm/jassub-worker.wasm", import.meta.url).href;
    const availableFonts = opts.availableFonts ?? {};
    if (!availableFonts["liberation sans"] && !opts.defaultFont) {
      availableFonts["liberation sans"] = new URL("./default.woff2", import.meta.url).href;
    }
    this.ready = (async () => {
      this.renderer = await new Renderer({
        wasmUrl: _JASSUB._supportsSIMD ? modern : normal,
        width: ctrl.width,
        height: ctrl.height,
        subUrl: opts.subUrl,
        subContent: opts.subContent ?? null,
        fonts: opts.fonts ?? [],
        availableFonts,
        defaultFont: opts.defaultFont ?? "liberation sans",
        debug: !!opts.debug,
        libassMemoryLimit: opts.libassMemoryLimit ?? 0,
        libassGlyphLimit: opts.libassGlyphLimit ?? 0,
        queryFonts: opts.queryFonts ?? "local"
      }, proxy((font) => this._getLocalFont(font)));
      await this.renderer.ready();
    })();
    if (this._video)
      this.setVideo(this._video);
    this._worker.postMessage({ name: "offscreenCanvas", ctrl }, [ctrl]);
  }
  static _supportsSIMD;
  static _test() {
    if (_JASSUB._supportsSIMD != null)
      return;
    try {
      _JASSUB._supportsSIMD = WebAssembly.validate(Uint8Array.of(0, 97, 115, 109, 1, 0, 0, 0, 1, 5, 1, 96, 0, 1, 123, 3, 2, 1, 0, 10, 10, 1, 8, 0, 65, 0, 253, 15, 253, 98, 11));
    } catch (e) {
      _JASSUB._supportsSIMD = false;
    }
    const module = new WebAssembly.Module(Uint8Array.of(0, 97, 115, 109, 1, 0, 0, 0));
    if (!(module instanceof WebAssembly.Module) || !(new WebAssembly.Instance(module) instanceof WebAssembly.Instance))
      throw new Error("WASM not supported");
  }
  async resize(forceRepaint = !!this._video?.paused, width = 0, height = 0, top = 0, left = 0) {
    if ((!width || !height) && this._video) {
      const videoSize = this._getVideoPosition();
      let renderSize = null;
      if (this._videoWidth) {
        const widthRatio = this._video.videoWidth / this._videoWidth;
        const heightRatio = this._video.videoHeight / this._videoHeight;
        renderSize = this._computeCanvasSize((videoSize.width || 0) / widthRatio, (videoSize.height || 0) / heightRatio);
      } else {
        renderSize = this._computeCanvasSize(videoSize.width || 0, videoSize.height || 0);
      }
      width = renderSize.width;
      height = renderSize.height;
      if (this._canvasParent) {
        top = videoSize.y - (this._canvasParent.getBoundingClientRect().top - this._video.getBoundingClientRect().top);
        left = videoSize.x;
      }
      this._canvas.style.width = videoSize.width + "px";
      this._canvas.style.height = videoSize.height + "px";
    }
    if (this._canvasParent) {
      this._canvas.style.top = top + "px";
      this._canvas.style.left = left + "px";
    }
    await this.renderer._resizeCanvas(width, height, (this._videoWidth || this._video?.videoWidth) ?? width, (this._videoHeight || this._video?.videoHeight) ?? height);
    if (this._lastDemandTime)
      await this._demandRender(forceRepaint);
  }
  _getVideoPosition(width = this._video.videoWidth, height = this._video.videoHeight) {
    const videoRatio = width / height;
    const { offsetWidth, offsetHeight } = this._video;
    const elementRatio = offsetWidth / offsetHeight;
    width = offsetWidth;
    height = offsetHeight;
    if (elementRatio > videoRatio) {
      width = Math.floor(offsetHeight * videoRatio);
    } else {
      height = Math.floor(offsetWidth / videoRatio);
    }
    const x = (offsetWidth - width) / 2;
    const y = (offsetHeight - height) / 2;
    return { width, height, x, y };
  }
  _computeCanvasSize(width = 0, height = 0) {
    const scalefactor = this.prescaleFactor <= 0 ? 1 : this.prescaleFactor;
    const ratio = self.devicePixelRatio || 1;
    if (height <= 0 || width <= 0) {
      width = 0;
      height = 0;
    } else {
      const sgn = scalefactor < 1 ? -1 : 1;
      let newH = height * ratio;
      if (sgn * newH * scalefactor <= sgn * this.prescaleHeightLimit) {
        newH *= scalefactor;
      } else if (sgn * newH < sgn * this.prescaleHeightLimit) {
        newH = this.prescaleHeightLimit;
      }
      if (this.maxRenderHeight > 0 && newH > this.maxRenderHeight)
        newH = this.maxRenderHeight;
      width *= newH / height;
      height = newH;
    }
    return { width, height };
  }
  async setVideo(video) {
    if (video instanceof HTMLVideoElement) {
      this._removeListeners();
      this._video = video;
      await this.ready;
      this._video.requestVideoFrameCallback((now, data) => this._handleRVFC(data));
      if ("VideoFrame" in globalThis) {
        video.addEventListener("loadedmetadata", () => this._updateColorSpace(), this._ctrl);
        if (video.readyState > 2)
          this._updateColorSpace();
      }
      if (video.videoWidth > 0)
        this.resize();
      this._ro.observe(video);
    } else {
      throw new Error("Video element invalid!");
    }
  }
  async _getLocalFont(font, weight = "regular") {
    if (navigator.permissions?.query) {
      const { state } = await navigator.permissions.query({ name: "local-fonts" });
      if (state !== "granted")
        return;
    }
    for (const data of await self.queryLocalFonts()) {
      const family = data.family.toLowerCase();
      const style = data.style.toLowerCase();
      if (family === font && style === weight) {
        const blob = await data.blob();
        return new Uint8Array(await blob.arrayBuffer());
      }
    }
  }
  _handleRVFC(data) {
    if (this._destroyed)
      return;
    this._lastDemandTime = data;
    this._demandRender();
    this._video.requestVideoFrameCallback((now, data2) => this._handleRVFC(data2));
  }
  async _demandRender(repaint = false) {
    const { mediaTime, width, height } = this._lastDemandTime;
    if (width !== this._videoWidth || height !== this._videoHeight) {
      this._videoWidth = width;
      this._videoHeight = height;
      return await this.resize(repaint);
    }
    if (this.busy) {
      this._skipped = true;
      this.debug?._drop();
      return;
    }
    this.busy = true;
    this._skipped = false;
    this.debug?._startFrame();
    await this.renderer._draw(mediaTime + this.timeOffset, repaint);
    this.debug?._endFrame(this._lastDemandTime);
    this.busy = false;
    if (this._skipped)
      await this._demandRender();
  }
  async _updateColorSpace() {
    await this.ready;
    this._video.requestVideoFrameCallback(async () => {
      try {
        const frame = new VideoFrame(this._video);
        frame.close();
        await this.renderer._setColorSpace(webYCbCrMap[frame.colorSpace.matrix]);
      } catch (e) {
        console.warn(e);
      }
    });
  }
  _removeListeners() {
    if (this._video) {
      if (this._ro)
        this._ro.unobserve(this._video);
      this._ctrl.abort();
      this._ctrl = new AbortController();
    }
  }
  async destroy() {
    if (this._destroyed)
      return;
    this._destroyed = true;
    if (this._video && this._canvasParent)
      this._video.parentNode?.removeChild(this._canvasParent);
    this._removeListeners();
    await this.renderer?.[releaseProxy]();
    this._worker.terminate();
  }
};
export {
  JASSUB as default
};
//# sourceMappingURL=jassub.js.map
