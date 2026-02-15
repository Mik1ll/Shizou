import {
  expose,
  finalizer
} from "../chunk-UEU5YF2M.js";
import {
  jassub_worker_default
} from "../chunk-7JMQBDTU.js";
import "../chunk-JSBRDJBE.js";

// node_modules/lfa-ponyfill/index.js
var SUPPORTS = "queryLocalFonts" in globalThis && globalThis.queryLocalFonts;
var WEIGHT_MAP = {
  100: "Thin",
  200: "ExtraLight",
  300: "Light",
  400: "Regular",
  // Normal isn't used
  500: "Medium",
  600: "SemiBold",
  700: "Bold",
  800: "ExtraBold",
  900: "Black",
  1e3: "UltraBlack"
};
var FontData = class {
  family;
  fullName;
  postscriptName;
  style;
  #weight;
  #isItalic;
  /** @param {{family: string, weight: string, isItalic: boolean, weightName: string}} param0 */
  constructor({ family, weight, isItalic, weightName }) {
    this.family = family;
    this.#weight = weight;
    this.#isItalic = isItalic;
    const isRegular = weightName === "Regular";
    if (isRegular) {
      if (isItalic) {
        this.style = "Italic";
      } else {
        this.style = "Regular";
      }
    } else {
      this.style = weightName + (isItalic ? " Italic" : "");
    }
    if (this.style === "Regular") {
      this.fullName = family;
    } else {
      this.fullName = `${family} ${this.style}`;
    }
    this.postscriptName = this.fullName.replace(/ /g, "-");
  }
  async blob() {
    const response = await fetch(`https://fonts.googleapis.com/css2?family=${this.family}:${this.#isItalic ? "ital," : ""}wght@${this.#isItalic ? "1," : ""}${this.#weight}`);
    const css = await response.text();
    const matches = [
      /\/\* latin-ext \*\/[\s\S]+url\(([^)]+)\)/,
      // get first latin woff2 url from css
      /\/\* latin \*\/[\s\S]+url\(([^)]+)\)/,
      // TODO: what if some1 wants a japanse font but it includes latin? "Noto Sans JP"
      /url\(([^)]+)\)/
    ];
    for (const match of matches) {
      const url = css.match(match)?.[1];
      if (url) {
        const response2 = await fetch(url);
        if (response2.ok) return response2.blob();
      }
    }
    throw new Error("Failed to load font blob");
  }
};
function fromMetadata([family, sizes]) {
  const fontData = [];
  for (const _type of sizes) {
    const type = "" + _type;
    const isItalic = type.endsWith("i");
    const weight = type.replace("i", "");
    if (weight.includes("a")) {
      const [min, max] = weight.split("a").map((a2) => parseInt(a2) * 100);
      for (let i = min; i <= max; i += 100) {
        fontData.push(new FontData({ family, weight: `${min}..${max}`, isItalic, weightName: WEIGHT_MAP[i] }));
      }
    } else {
      fontData.push(new FontData({ family, weight: `${weight}00`, isItalic, weightName: WEIGHT_MAP[parseInt(weight) * 100] }));
    }
  }
  return fontData;
}
var fontCache = /* @__PURE__ */ new Map();
async function getFontCache() {
  if (fontCache.size === 0) {
    const fonts = await import("../fonts-PN3EKUGO.js");
    for (const font of fonts.default.flatMap((familyMetadata) => fromMetadata(familyMetadata))) {
      fontCache.set(font.postscriptName.toLowerCase().replace(/-/g, ""), font);
    }
  }
  return fontCache;
}
async function queryRemoteFonts({ postscriptNames } = {}) {
  const fontCache2 = await getFontCache();
  if (!postscriptNames) return [...fontCache2.values()];
  if (postscriptNames.length === 0) return [];
  return postscriptNames.reduce(
    (acc, postscriptName) => {
      const font = fontCache2.get(postscriptName.toLowerCase().replace(/regular$/, "").replace(/-reg$/, "").replace(/[- ]/g, ""));
      if (font) acc.push(font);
      return acc;
    },
    /** @type {FontData[]} */
    []
  );
}

// node_modules/jassub/dist/worker/renderers/2d-renderer.js
var Canvas2DRenderer = class {
  canvas = null;
  ctx = null;
  bufferCanvas = new OffscreenCanvas(1, 1);
  bufferCtx = this.bufferCanvas.getContext("2d", {
    alpha: true,
    desynchronized: true,
    willReadFrequently: false
  });
  _scheduledResize;
  resizeCanvas(width, height) {
    if (width <= 0 || height <= 0)
      return;
    this._scheduledResize = { width, height };
  }
  setCanvas(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext("2d", {
      alpha: true,
      desynchronized: true,
      willReadFrequently: false
    });
    if (!this.ctx)
      throw new Error("Could not get 2D context");
  }
  setColorMatrix(subtitleColorSpace, videoColorSpace) {
  }
  // this is horribly inefficient, but it's a fallback for systems without a GPU, this is the least of their problems
  render(images, heap) {
    if (!this.ctx || !this.canvas)
      return;
    if (this._scheduledResize) {
      const { width, height } = this._scheduledResize;
      this._scheduledResize = void 0;
      this.canvas.width = width;
      this.canvas.height = height;
    } else {
      this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }
    for (const img of images) {
      if (img.w <= 0 || img.h <= 0)
        continue;
      const imageData = new ImageData(img.w, img.h);
      const pixels = new Uint32Array(imageData.data.buffer);
      const color = img.color << 8 & 16711680 | img.color >> 8 & 65280 | img.color >> 24 & 255;
      const alpha = (255 - (img.color & 255)) / 255;
      const stride = img.stride;
      const h = img.h;
      const w = img.w;
      for (let y = h + 1, pos = img.bitmap, res = 0; --y; pos += stride) {
        for (let z = 0; z < w; ++z, ++res) {
          const k = heap[pos + z];
          if (k !== 0)
            pixels[res] = alpha * k << 24 | color;
        }
      }
      this.bufferCanvas.width = w;
      this.bufferCanvas.height = h;
      this.bufferCtx.putImageData(imageData, 0, 0);
      this.ctx.drawImage(this.bufferCanvas, img.dst_x, img.dst_y);
    }
  }
  destroy() {
    this.ctx = null;
    this.canvas = null;
    this.bufferCtx = null;
    this.bufferCanvas = null;
  }
};

// node_modules/jassub/dist/worker/util.js
var WEIGHT_MAP2 = [
  "thin",
  "extralight",
  "light",
  "regular",
  // Normal isn't used
  "medium",
  "semibold",
  "bold",
  "extrabold",
  "black",
  "ultrablack"
];
var IS_FIREFOX = navigator.userAgent.toLowerCase().includes("firefox");
var a = "BT601";
var b = "BT709";
var c = "SMPTE240M";
var d = "FCC";
var LIBASS_YCBCR_MAP = [null, a, null, a, a, b, b, c, c, d, d];
function _applyKeys(input, output) {
  for (const v of Object.keys(input)) {
    output[v] = input[v];
  }
}
var _fetch = globalThis.fetch;
async function fetchtext(url) {
  const res = await _fetch(url);
  return await res.text();
}
var THREAD_COUNT = !IS_FIREFOX && self.crossOriginIsolated ? Math.min(Math.max(1, navigator.hardwareConcurrency - 2), 8) : 1;
var SUPPORTS_GROWTH = !!WebAssembly.Memory.prototype.toResizableBuffer;
var SHOULD_REFERENCE_MEMORY = !IS_FIREFOX && (SUPPORTS_GROWTH || THREAD_COUNT > 1);
var IDENTITY_MATRIX = new Float32Array([
  1,
  0,
  0,
  0,
  1,
  0,
  0,
  0,
  1
]);
var colorMatrixConversionMap = {
  BT601: {
    BT709: new Float32Array([
      1.0863,
      0.0965,
      -0.01411,
      -0.0723,
      0.8451,
      -0.0277,
      -0.0141,
      0.0584,
      1.0418
    ]),
    BT601: IDENTITY_MATRIX
  },
  BT709: {
    BT601: new Float32Array([
      0.9137,
      0.0784,
      79e-4,
      -0.1049,
      1.1722,
      -0.0671,
      96e-4,
      0.0322,
      0.9582
    ]),
    BT709: IDENTITY_MATRIX
  },
  FCC: {
    BT709: new Float32Array([
      1.0873,
      -0.0736,
      -0.0137,
      0.0974,
      0.8494,
      0.0531,
      -0.0127,
      -0.0251,
      1.0378
    ]),
    BT601: new Float32Array([
      1.001,
      -8e-4,
      -2e-4,
      9e-4,
      1.005,
      -6e-3,
      13e-4,
      27e-4,
      0.996
    ])
  },
  SMPTE240M: {
    BT709: new Float32Array([
      0.9993,
      6e-4,
      1e-4,
      -4e-4,
      0.9812,
      0.0192,
      -34e-4,
      -0.0114,
      1.0148
    ]),
    BT601: new Float32Array([
      0.913,
      0.0774,
      96e-4,
      -0.1051,
      1.1508,
      -0.0456,
      63e-4,
      0.0207,
      0.973
    ])
  }
};

// node_modules/jassub/dist/worker/renderers/webgl1-renderer.js
var VERTEX_SHADER = (
  /* glsl */
  `
precision mediump float;

// Quad position attribute (0,0), (1,0), (0,1), (1,0), (1,1), (0,1)
attribute vec2 a_quadPos;

uniform vec2 u_resolution;

// Instance attributes
attribute vec4 a_destRect;  // x, y, w, h
attribute vec4 a_color;     // r, g, b, a
attribute float a_texLayer;

varying vec2 v_destXY;
varying vec4 v_color;
varying vec2 v_texSize;
varying float v_texLayer;
varying vec2 v_texCoord;

void main() {
  vec2 pixelPos = a_destRect.xy + a_quadPos * a_destRect.zw;
  vec2 clipPos = (pixelPos / u_resolution) * 2.0 - 1.0;
  clipPos.y = -clipPos.y;

  gl_Position = vec4(clipPos, 0.0, 1.0);
  v_destXY = a_destRect.xy;
  v_color = a_color;
  v_texSize = a_destRect.zw;
  v_texLayer = a_texLayer;
  v_texCoord = a_quadPos;
}
`
);
var FRAGMENT_SHADER = (
  /* glsl */
  `
precision mediump float;

uniform sampler2D u_tex;
uniform mat3 u_colorMatrix;
uniform vec2 u_resolution;
uniform vec2 u_texDimensions; // Actual texture dimensions

varying vec2 v_destXY;
varying vec4 v_color;
varying vec2 v_texSize;
varying float v_texLayer;
varying vec2 v_texCoord;

void main() {
  // v_texCoord is in 0-1 range for the quad
  // We need to map it to the actual image size within the texture
  // The image occupies only (v_texSize.x / u_texDimensions.x, v_texSize.y / u_texDimensions.y) of the texture
  vec2 normalizedImageSize = v_texSize / u_texDimensions;
  vec2 texCoord = v_texCoord * normalizedImageSize;

  // Sample texture (r channel contains mask)
  float mask = texture2D(u_tex, texCoord).r;

  // Apply color matrix conversion (identity if no conversion needed)
  vec3 correctedColor = u_colorMatrix * v_color.rgb;

  // libass color alpha: 0 = opaque, 255 = transparent (inverted)
  float colorAlpha = 1.0 - v_color.a;

  // Final alpha = colorAlpha * mask
  float a = colorAlpha * mask;

  // Premultiplied alpha output
  gl_FragColor = vec4(correctedColor * a, a);
}
`
);
var MAX_INSTANCES = 256;
var WebGL1Renderer = class {
  canvas = null;
  gl = null;
  program = null;
  // Extensions
  instancedArraysExt = null;
  // Uniform locations
  u_resolution = null;
  u_tex = null;
  u_colorMatrix = null;
  u_texDimensions = null;
  // Attribute locations
  a_quadPos = -1;
  a_destRect = -1;
  a_color = -1;
  a_texLayer = -1;
  // Quad vertex buffer (shared for all instances)
  quadPosBuffer = null;
  // Instance attribute buffers
  instanceDestRectBuffer = null;
  instanceColorBuffer = null;
  instanceTexLayerBuffer = null;
  // Instance data arrays
  instanceDestRectData;
  instanceColorData;
  instanceTexLayerData;
  // Texture cache (since WebGL1 doesn't support texture arrays)
  textureCache = /* @__PURE__ */ new Map();
  textureWidth = 0;
  textureHeight = 0;
  colorMatrix = IDENTITY_MATRIX;
  constructor() {
    this.instanceDestRectData = new Float32Array(MAX_INSTANCES * 4);
    this.instanceColorData = new Float32Array(MAX_INSTANCES * 4);
    this.instanceTexLayerData = new Float32Array(MAX_INSTANCES);
  }
  _scheduledResize;
  resizeCanvas(width, height) {
    if (width <= 0 || height <= 0)
      return;
    this._scheduledResize = { width, height };
  }
  setCanvas(canvas) {
    this.canvas = canvas;
    this.gl = canvas.getContext("webgl", {
      alpha: true,
      premultipliedAlpha: true,
      antialias: false,
      depth: false,
      preserveDrawingBuffer: false,
      stencil: false,
      desynchronized: true,
      powerPreference: "high-performance"
    });
    if (!this.gl) {
      throw new Error("Could not get WebGL context");
    }
    this.instancedArraysExt = this.gl.getExtension("ANGLE_instanced_arrays");
    if (!this.instancedArraysExt) {
      throw new Error("ANGLE_instanced_arrays extension not supported");
    }
    const vertexShader = this.createShader(this.gl.VERTEX_SHADER, VERTEX_SHADER);
    const fragmentShader = this.createShader(this.gl.FRAGMENT_SHADER, FRAGMENT_SHADER);
    if (!vertexShader || !fragmentShader) {
      throw new Error("Failed to create shaders");
    }
    this.program = this.gl.createProgram();
    this.gl.attachShader(this.program, vertexShader);
    this.gl.attachShader(this.program, fragmentShader);
    this.gl.linkProgram(this.program);
    if (!this.gl.getProgramParameter(this.program, this.gl.LINK_STATUS)) {
      const info = this.gl.getProgramInfoLog(this.program);
      throw new Error("Failed to link program: " + info);
    }
    this.u_resolution = this.gl.getUniformLocation(this.program, "u_resolution");
    this.u_tex = this.gl.getUniformLocation(this.program, "u_tex");
    this.u_colorMatrix = this.gl.getUniformLocation(this.program, "u_colorMatrix");
    this.u_texDimensions = this.gl.getUniformLocation(this.program, "u_texDimensions");
    this.a_quadPos = this.gl.getAttribLocation(this.program, "a_quadPos");
    this.a_destRect = this.gl.getAttribLocation(this.program, "a_destRect");
    this.a_color = this.gl.getAttribLocation(this.program, "a_color");
    this.a_texLayer = this.gl.getAttribLocation(this.program, "a_texLayer");
    this.quadPosBuffer = this.gl.createBuffer();
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.quadPosBuffer);
    const quadPositions = new Float32Array([
      0,
      0,
      1,
      0,
      0,
      1,
      1,
      0,
      1,
      1,
      0,
      1
    ]);
    this.gl.bufferData(this.gl.ARRAY_BUFFER, quadPositions, this.gl.STATIC_DRAW);
    this.instanceDestRectBuffer = this.gl.createBuffer();
    this.instanceColorBuffer = this.gl.createBuffer();
    this.instanceTexLayerBuffer = this.gl.createBuffer();
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.quadPosBuffer);
    this.gl.enableVertexAttribArray(this.a_quadPos);
    this.gl.vertexAttribPointer(this.a_quadPos, 2, this.gl.FLOAT, false, 0, 0);
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceDestRectBuffer);
    this.gl.enableVertexAttribArray(this.a_destRect);
    this.gl.vertexAttribPointer(this.a_destRect, 4, this.gl.FLOAT, false, 0, 0);
    this.instancedArraysExt.vertexAttribDivisorANGLE(this.a_destRect, 1);
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceColorBuffer);
    this.gl.enableVertexAttribArray(this.a_color);
    this.gl.vertexAttribPointer(this.a_color, 4, this.gl.FLOAT, false, 0, 0);
    this.instancedArraysExt.vertexAttribDivisorANGLE(this.a_color, 1);
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceTexLayerBuffer);
    this.gl.enableVertexAttribArray(this.a_texLayer);
    this.gl.vertexAttribPointer(this.a_texLayer, 1, this.gl.FLOAT, false, 0, 0);
    this.instancedArraysExt.vertexAttribDivisorANGLE(this.a_texLayer, 1);
    this.gl.enable(this.gl.BLEND);
    this.gl.blendFunc(this.gl.ONE, this.gl.ONE_MINUS_SRC_ALPHA);
    this.gl.useProgram(this.program);
    this.gl.uniform1i(this.u_tex, 0);
    this.gl.uniformMatrix3fv(this.u_colorMatrix, false, this.colorMatrix);
    this.gl.pixelStorei(this.gl.UNPACK_ALIGNMENT, 1);
    this.gl.clearColor(0, 0, 0, 0);
    this.gl.activeTexture(this.gl.TEXTURE0);
  }
  createShader(type, source) {
    const shader = this.gl.createShader(type);
    this.gl.shaderSource(shader, source);
    this.gl.compileShader(shader);
    if (!this.gl.getShaderParameter(shader, this.gl.COMPILE_STATUS)) {
      const info = this.gl.getShaderInfoLog(shader);
      console.log(info);
      this.gl.deleteShader(shader);
      return null;
    }
    return shader;
  }
  // Set the color matrix for color space conversion.
  // Pass null or undefined to use identity (no conversion).
  setColorMatrix(subtitleColorSpace, videoColorSpace) {
    this.colorMatrix = (subtitleColorSpace && videoColorSpace && colorMatrixConversionMap[subtitleColorSpace]?.[videoColorSpace]) ?? IDENTITY_MATRIX;
    if (this.gl && this.u_colorMatrix && this.program) {
      this.gl.useProgram(this.program);
      this.gl.uniformMatrix3fv(this.u_colorMatrix, false, this.colorMatrix);
    }
  }
  createTexture(width, height) {
    const texture = this.gl.createTexture();
    this.gl.bindTexture(this.gl.TEXTURE_2D, texture);
    this.gl.texImage2D(this.gl.TEXTURE_2D, 0, this.gl.LUMINANCE, width, height, 0, this.gl.LUMINANCE, this.gl.UNSIGNED_BYTE, null);
    this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_MIN_FILTER, this.gl.NEAREST);
    this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_MAG_FILTER, this.gl.NEAREST);
    this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_WRAP_S, this.gl.CLAMP_TO_EDGE);
    this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_WRAP_T, this.gl.CLAMP_TO_EDGE);
    return texture;
  }
  render(images, heap) {
    if (!this.gl || !this.program || !this.instancedArraysExt)
      return;
    if (this._scheduledResize) {
      const { width, height } = this._scheduledResize;
      this._scheduledResize = void 0;
      this.canvas.width = width;
      this.canvas.height = height;
      this.gl.viewport(0, 0, width, height);
      this.gl.uniform2f(this.u_resolution, width, height);
    } else {
      this.gl.clear(this.gl.COLOR_BUFFER_BIT);
    }
    let maxW = this.textureWidth;
    let maxH = this.textureHeight;
    const validImages = [];
    for (const img of images) {
      if (img.w <= 0 || img.h <= 0)
        continue;
      validImages.push(img);
      if (img.w > maxW)
        maxW = img.w;
      if (img.h > maxH)
        maxH = img.h;
    }
    if (validImages.length === 0)
      return;
    if (maxW > this.textureWidth || maxH > this.textureHeight) {
      this.textureWidth = maxW;
      this.textureHeight = maxH;
      for (const texture of this.textureCache.values()) {
        this.gl.deleteTexture(texture);
      }
      this.textureCache.clear();
    }
    for (let i = 0; i < validImages.length; i++) {
      const img = validImages[i];
      let texture = this.textureCache.get(i);
      if (!texture) {
        texture = this.createTexture(this.textureWidth, this.textureHeight);
        this.textureCache.set(i, texture);
      }
      this.gl.bindTexture(this.gl.TEXTURE_2D, texture);
      const sourceView = new Uint8Array(heap.buffer, img.bitmap, img.stride * img.h);
      const tightData = new Uint8Array(img.w * img.h);
      for (let y = 0; y < img.h; y++) {
        const srcOffset = y * img.stride;
        const dstOffset = y * img.w;
        tightData.set(sourceView.subarray(srcOffset, srcOffset + img.w), dstOffset);
      }
      this.gl.texSubImage2D(
        this.gl.TEXTURE_2D,
        0,
        0,
        0,
        // x, y offset
        img.w,
        img.h,
        this.gl.LUMINANCE,
        this.gl.UNSIGNED_BYTE,
        tightData
      );
      this.instanceDestRectData[0] = img.dst_x;
      this.instanceDestRectData[1] = img.dst_y;
      this.instanceDestRectData[2] = img.w;
      this.instanceDestRectData[3] = img.h;
      this.instanceColorData[0] = (img.color >>> 24 & 255) / 255;
      this.instanceColorData[1] = (img.color >>> 16 & 255) / 255;
      this.instanceColorData[2] = (img.color >>> 8 & 255) / 255;
      this.instanceColorData[3] = (img.color & 255) / 255;
      this.instanceTexLayerData[0] = 0;
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceDestRectBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceDestRectData.subarray(0, 4), this.gl.DYNAMIC_DRAW);
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceColorBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceColorData.subarray(0, 4), this.gl.DYNAMIC_DRAW);
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceTexLayerBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceTexLayerData.subarray(0, 1), this.gl.DYNAMIC_DRAW);
      this.gl.uniform2f(this.u_texDimensions, this.textureWidth, this.textureHeight);
      this.instancedArraysExt.drawArraysInstancedANGLE(this.gl.TRIANGLES, 0, 6, 1);
    }
  }
  destroy() {
    if (this.gl) {
      for (const texture of this.textureCache.values()) {
        this.gl.deleteTexture(texture);
      }
      this.textureCache.clear();
      if (this.quadPosBuffer) {
        this.gl.deleteBuffer(this.quadPosBuffer);
        this.quadPosBuffer = null;
      }
      if (this.instanceDestRectBuffer) {
        this.gl.deleteBuffer(this.instanceDestRectBuffer);
        this.instanceDestRectBuffer = null;
      }
      if (this.instanceColorBuffer) {
        this.gl.deleteBuffer(this.instanceColorBuffer);
        this.instanceColorBuffer = null;
      }
      if (this.instanceTexLayerBuffer) {
        this.gl.deleteBuffer(this.instanceTexLayerBuffer);
        this.instanceTexLayerBuffer = null;
      }
      if (this.program) {
        this.gl.deleteProgram(this.program);
        this.program = null;
      }
      this.gl = null;
    }
  }
};

// node_modules/jassub/dist/worker/renderers/webgl2-renderer.js
var VERTEX_SHADER2 = (
  /* glsl */
  `#version 300 es
precision mediump float;

const vec2 QUAD_POSITIONS[6] = vec2[6](
  vec2(0.0, 0.0),
  vec2(1.0, 0.0),
  vec2(0.0, 1.0),
  vec2(1.0, 0.0),
  vec2(1.0, 1.0),
  vec2(0.0, 1.0)
);

uniform vec2 u_resolution;

// Instance attributes
in vec4 a_destRect;  // x, y, w, h
in vec4 a_color;     // r, g, b, a
in float a_texLayer;

flat out vec2 v_destXY;
flat out vec4 v_color;
flat out vec2 v_texSize;
flat out float v_texLayer;

void main() {
  vec2 quadPos = QUAD_POSITIONS[gl_VertexID];
  vec2 pixelPos = a_destRect.xy + quadPos * a_destRect.zw;
  vec2 clipPos = (pixelPos / u_resolution) * 2.0 - 1.0;
  clipPos.y = -clipPos.y;

  gl_Position = vec4(clipPos, 0.0, 1.0);
  v_destXY = a_destRect.xy;
  v_color = a_color;
  v_texSize = a_destRect.zw;
  v_texLayer = a_texLayer;
}
`
);
var FRAGMENT_SHADER2 = (
  /* glsl */
  `#version 300 es
precision mediump float;
precision mediump sampler2DArray;

uniform sampler2DArray u_texArray;
uniform mat3 u_colorMatrix;
uniform vec2 u_resolution;

flat in vec2 v_destXY;
flat in vec4 v_color;
flat in vec2 v_texSize;
flat in float v_texLayer;

out vec4 fragColor;

void main() {
  // Flip Y: WebGL's gl_FragCoord.y is 0 at bottom, but destXY.y is from top
  vec2 fragPos = vec2(gl_FragCoord.x, u_resolution.y - gl_FragCoord.y);

  // Calculate local position within the quad (screen coords)
  vec2 localPos = fragPos - v_destXY;

  // Convert to integer texel coordinates for texelFetch
  ivec2 texCoord = ivec2(floor(localPos));

  // Bounds check (prevents out-of-bounds access)
  ivec2 texSizeI = ivec2(v_texSize);
  if (texCoord.x < 0 || texCoord.y < 0 || texCoord.x >= texSizeI.x || texCoord.y >= texSizeI.y) {
    discard;
  }

  // texelFetch: integer coords, no interpolation, no precision issues
  float mask = texelFetch(u_texArray, ivec3(texCoord, int(v_texLayer)), 0).r;

  // Apply color matrix conversion (identity if no conversion needed)
  vec3 correctedColor = u_colorMatrix * v_color.rgb;

  // libass color alpha: 0 = opaque, 255 = transparent (inverted)
  float colorAlpha = 1.0 - v_color.a;

  // Final alpha = colorAlpha * mask
  float a = colorAlpha * mask;

  // Premultiplied alpha output
  fragColor = vec4(correctedColor * a, a);
}
`
);
var TEX_ARRAY_SIZE = 64;
var TEX_INITIAL_SIZE = 256;
var MAX_INSTANCES2 = 256;
var WebGL2Renderer = class {
  canvas = null;
  gl = null;
  program = null;
  vao = null;
  // Uniform locations
  u_resolution = null;
  u_texArray = null;
  u_colorMatrix = null;
  // Instance attribute buffers
  instanceDestRectBuffer = null;
  instanceColorBuffer = null;
  instanceTexLayerBuffer = null;
  // Instance data arrays
  instanceDestRectData;
  instanceColorData;
  instanceTexLayerData;
  texArray = null;
  texArrayWidth = 0;
  texArrayHeight = 0;
  colorMatrix = IDENTITY_MATRIX;
  constructor() {
    this.instanceDestRectData = new Float32Array(MAX_INSTANCES2 * 4);
    this.instanceColorData = new Float32Array(MAX_INSTANCES2 * 4);
    this.instanceTexLayerData = new Float32Array(MAX_INSTANCES2);
  }
  _scheduledResize;
  resizeCanvas(width, height) {
    if (width <= 0 || height <= 0)
      return;
    this._scheduledResize = { width, height };
  }
  setCanvas(canvas) {
    this.canvas = canvas;
    this.gl = canvas.getContext("webgl2", {
      alpha: true,
      premultipliedAlpha: true,
      antialias: false,
      depth: false,
      preserveDrawingBuffer: false,
      stencil: false,
      desynchronized: true,
      powerPreference: "high-performance"
    });
    if (!this.gl) {
      throw new Error("Could not get WebGL2 context");
    }
    const vertexShader = this.createShader(this.gl.VERTEX_SHADER, VERTEX_SHADER2);
    const fragmentShader = this.createShader(this.gl.FRAGMENT_SHADER, FRAGMENT_SHADER2);
    if (!vertexShader || !fragmentShader) {
      throw new Error("Failed to create shaders");
    }
    this.program = this.gl.createProgram();
    this.gl.attachShader(this.program, vertexShader);
    this.gl.attachShader(this.program, fragmentShader);
    this.gl.linkProgram(this.program);
    if (!this.gl.getProgramParameter(this.program, this.gl.LINK_STATUS)) {
      const info = this.gl.getProgramInfoLog(this.program);
      throw new Error("Failed to link program: " + info);
    }
    this.gl.deleteShader(vertexShader);
    this.gl.deleteShader(fragmentShader);
    this.u_resolution = this.gl.getUniformLocation(this.program, "u_resolution");
    this.u_texArray = this.gl.getUniformLocation(this.program, "u_texArray");
    this.u_colorMatrix = this.gl.getUniformLocation(this.program, "u_colorMatrix");
    this.instanceDestRectBuffer = this.gl.createBuffer();
    this.instanceColorBuffer = this.gl.createBuffer();
    this.instanceTexLayerBuffer = this.gl.createBuffer();
    this.vao = this.gl.createVertexArray();
    this.gl.bindVertexArray(this.vao);
    const destRectLoc = this.gl.getAttribLocation(this.program, "a_destRect");
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceDestRectBuffer);
    this.gl.enableVertexAttribArray(destRectLoc);
    this.gl.vertexAttribPointer(destRectLoc, 4, this.gl.FLOAT, false, 0, 0);
    this.gl.vertexAttribDivisor(destRectLoc, 1);
    const colorLoc = this.gl.getAttribLocation(this.program, "a_color");
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceColorBuffer);
    this.gl.enableVertexAttribArray(colorLoc);
    this.gl.vertexAttribPointer(colorLoc, 4, this.gl.FLOAT, false, 0, 0);
    this.gl.vertexAttribDivisor(colorLoc, 1);
    const texLayerLoc = this.gl.getAttribLocation(this.program, "a_texLayer");
    this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceTexLayerBuffer);
    this.gl.enableVertexAttribArray(texLayerLoc);
    this.gl.vertexAttribPointer(texLayerLoc, 1, this.gl.FLOAT, false, 0, 0);
    this.gl.vertexAttribDivisor(texLayerLoc, 1);
    this.gl.enable(this.gl.BLEND);
    this.gl.blendFunc(this.gl.ONE, this.gl.ONE_MINUS_SRC_ALPHA);
    this.gl.useProgram(this.program);
    this.gl.uniform1i(this.u_texArray, 0);
    this.gl.uniformMatrix3fv(this.u_colorMatrix, false, this.colorMatrix);
    this.gl.pixelStorei(this.gl.UNPACK_ALIGNMENT, 1);
    this.gl.clearColor(0, 0, 0, 0);
    this.gl.activeTexture(this.gl.TEXTURE0);
    this.createTexArray(TEX_INITIAL_SIZE, TEX_INITIAL_SIZE);
  }
  createShader(type, source) {
    const shader = this.gl.createShader(type);
    this.gl.shaderSource(shader, source);
    this.gl.compileShader(shader);
    if (!this.gl.getShaderParameter(shader, this.gl.COMPILE_STATUS)) {
      const info = this.gl.getShaderInfoLog(shader);
      console.log(info);
      this.gl.deleteShader(shader);
      return null;
    }
    return shader;
  }
  // Set the color matrix for color space conversion.
  // Pass null or undefined to use identity (no conversion).
  setColorMatrix(subtitleColorSpace, videoColorSpace) {
    this.colorMatrix = (subtitleColorSpace && videoColorSpace && colorMatrixConversionMap[subtitleColorSpace]?.[videoColorSpace]) ?? IDENTITY_MATRIX;
    if (this.gl && this.u_colorMatrix && this.program) {
      this.gl.useProgram(this.program);
      this.gl.uniformMatrix3fv(this.u_colorMatrix, false, this.colorMatrix);
    }
  }
  createTexArray(width, height) {
    if (this.texArray) {
      this.gl.deleteTexture(this.texArray);
    }
    this.texArray = this.gl.createTexture();
    this.gl.bindTexture(this.gl.TEXTURE_2D_ARRAY, this.texArray);
    this.gl.texImage3D(
      this.gl.TEXTURE_2D_ARRAY,
      0,
      this.gl.R8,
      width,
      height,
      TEX_ARRAY_SIZE,
      0,
      this.gl.RED,
      this.gl.UNSIGNED_BYTE,
      null
      // Firefox cries about uninitialized data, but is slower with zero initialized data...
    );
    this.gl.texParameteri(this.gl.TEXTURE_2D_ARRAY, this.gl.TEXTURE_MIN_FILTER, this.gl.NEAREST);
    this.gl.texParameteri(this.gl.TEXTURE_2D_ARRAY, this.gl.TEXTURE_MAG_FILTER, this.gl.NEAREST);
    this.gl.texParameteri(this.gl.TEXTURE_2D_ARRAY, this.gl.TEXTURE_WRAP_S, this.gl.CLAMP_TO_EDGE);
    this.gl.texParameteri(this.gl.TEXTURE_2D_ARRAY, this.gl.TEXTURE_WRAP_T, this.gl.CLAMP_TO_EDGE);
    this.texArrayWidth = width;
    this.texArrayHeight = height;
  }
  render(images, heap) {
    if (!this.gl || !this.program || !this.vao || !this.texArray)
      return;
    if (self.HEAPU8RAW.buffer !== self.WASMMEMORY.buffer || SHOULD_REFERENCE_MEMORY) {
      heap = self.HEAPU8RAW = new Uint8Array(self.WASMMEMORY.buffer);
    }
    if (this._scheduledResize) {
      const { width, height } = this._scheduledResize;
      this._scheduledResize = void 0;
      this.canvas.width = width;
      this.canvas.height = height;
      this.gl.viewport(0, 0, width, height);
      this.gl.uniform2f(this.u_resolution, width, height);
    } else {
      this.gl.clear(this.gl.COLOR_BUFFER_BIT);
    }
    let maxW = this.texArrayWidth;
    let maxH = this.texArrayHeight;
    const validImages = [];
    for (const img of images) {
      if (img.w <= 0 || img.h <= 0)
        continue;
      validImages.push(img);
      if (img.w > maxW)
        maxW = img.w;
      if (img.h > maxH)
        maxH = img.h;
    }
    if (validImages.length === 0)
      return;
    if (maxW > this.texArrayWidth || maxH > this.texArrayHeight) {
      this.createTexArray(maxW, maxH);
    }
    const batchSize = Math.min(TEX_ARRAY_SIZE, MAX_INSTANCES2);
    for (let batchStart = 0; batchStart < validImages.length; batchStart += batchSize) {
      const batchEnd = Math.min(batchStart + batchSize, validImages.length);
      let instanceCount = 0;
      for (let i = batchStart; i < batchEnd; i++) {
        const img = validImages[i];
        const layer = instanceCount;
        this.gl.pixelStorei(this.gl.UNPACK_ROW_LENGTH, img.stride);
        if (IS_FIREFOX) {
          const sourceView = new Uint8Array(heap.buffer, img.bitmap, img.stride * img.h);
          const bitmapData = new Uint8Array(sourceView);
          this.gl.texSubImage3D(
            this.gl.TEXTURE_2D_ARRAY,
            0,
            0,
            0,
            layer,
            // x, y, z offset
            img.w,
            img.h,
            1,
            // depth (1 layer)
            this.gl.RED,
            this.gl.UNSIGNED_BYTE,
            bitmapData
          );
        } else {
          this.gl.texSubImage3D(
            this.gl.TEXTURE_2D_ARRAY,
            0,
            0,
            0,
            layer,
            // x, y, z offset
            img.w,
            img.h,
            1,
            // depth (1 layer)
            this.gl.RED,
            this.gl.UNSIGNED_BYTE,
            heap,
            img.bitmap
          );
        }
        const idx = instanceCount * 4;
        this.instanceDestRectData[idx] = img.dst_x;
        this.instanceDestRectData[idx + 1] = img.dst_y;
        this.instanceDestRectData[idx + 2] = img.w;
        this.instanceDestRectData[idx + 3] = img.h;
        this.instanceColorData[idx] = (img.color >>> 24 & 255) / 255;
        this.instanceColorData[idx + 1] = (img.color >>> 16 & 255) / 255;
        this.instanceColorData[idx + 2] = (img.color >>> 8 & 255) / 255;
        this.instanceColorData[idx + 3] = (img.color & 255) / 255;
        this.instanceTexLayerData[instanceCount] = layer;
        instanceCount++;
      }
      this.gl.pixelStorei(this.gl.UNPACK_ROW_LENGTH, 0);
      if (instanceCount === 0)
        continue;
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceDestRectBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceDestRectData.subarray(0, instanceCount * 4), this.gl.DYNAMIC_DRAW);
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceColorBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceColorData.subarray(0, instanceCount * 4), this.gl.DYNAMIC_DRAW);
      this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.instanceTexLayerBuffer);
      this.gl.bufferData(this.gl.ARRAY_BUFFER, this.instanceTexLayerData.subarray(0, instanceCount), this.gl.DYNAMIC_DRAW);
      this.gl.drawArraysInstanced(this.gl.TRIANGLES, 0, 6, instanceCount);
    }
  }
  destroy() {
    if (this.gl) {
      if (this.texArray) {
        this.gl.deleteTexture(this.texArray);
        this.texArray = null;
      }
      if (this.instanceDestRectBuffer) {
        this.gl.deleteBuffer(this.instanceDestRectBuffer);
        this.instanceDestRectBuffer = null;
      }
      if (this.instanceColorBuffer) {
        this.gl.deleteBuffer(this.instanceColorBuffer);
        this.instanceColorBuffer = null;
      }
      if (this.instanceTexLayerBuffer) {
        this.gl.deleteBuffer(this.instanceTexLayerBuffer);
        this.instanceTexLayerBuffer = null;
      }
      if (this.vao) {
        this.gl.deleteVertexArray(this.vao);
        this.vao = null;
      }
      if (this.program) {
        this.gl.deleteProgram(this.program);
        this.program = null;
      }
      this.gl = null;
    }
  }
};

// node_modules/jassub/dist/worker/worker.js
var ASSRenderer = class {
  _offCanvas;
  _wasm;
  _subtitleColorSpace;
  _videoColorSpace;
  _malloc;
  _gpurender;
  debug = false;
  _ready;
  constructor(data, getFont) {
    this._availableFonts = Object.fromEntries(Object.entries(data.availableFonts).map(([k, v]) => [k.trim().toLowerCase(), v]));
    this.debug = data.debug;
    this.queryFonts = data.queryFonts;
    this._getFont = getFont;
    this._defaultFont = data.defaultFont.trim().toLowerCase();
    const _fetch2 = globalThis.fetch;
    globalThis.fetch = (_) => _fetch2(data.wasmUrl);
    const handleMessage = ({ data: data2 }) => {
      if (data2.name === "offscreenCanvas") {
        this._offCanvas = data2.ctrl;
        this._gpurender.setCanvas(this._offCanvas);
        removeEventListener("message", handleMessage);
      }
    };
    addEventListener("message", handleMessage);
    try {
      const testCanvas = new OffscreenCanvas(1, 1);
      if (testCanvas.getContext("webgl2")) {
        this._gpurender = new WebGL2Renderer();
      } else {
        this._gpurender = testCanvas.getContext("webgl")?.getExtension("ANGLE_instanced_arrays") ? new WebGL1Renderer() : new Canvas2DRenderer();
      }
    } catch {
      this._gpurender = new Canvas2DRenderer();
    }
    this._ready = jassub_worker_default({ __url: data.wasmUrl, __out: (log) => this._log(log) }).then(async ({ _malloc, JASSUB }) => {
      this._malloc = _malloc;
      this._wasm = new JASSUB(data.width, data.height, this._defaultFont);
      this._wasm.setThreads(THREAD_COUNT);
      this._loadInitialFonts(data.fonts);
      this._wasm.createTrackMem(data.subContent ?? await fetchtext(data.subUrl));
      this._subtitleColorSpace = LIBASS_YCBCR_MAP[this._wasm.trackColorSpace];
      if (data.libassMemoryLimit > 0 || data.libassGlyphLimit > 0) {
        this._wasm.setMemoryLimits(data.libassGlyphLimit || 0, data.libassMemoryLimit || 0);
      }
      this._checkColorSpace();
    });
  }
  ready() {
    return this._ready;
  }
  createEvent(event) {
    _applyKeys(event, this._wasm.getEvent(this._wasm.allocEvent()));
  }
  getEvents() {
    const events = [];
    for (let i = 0; i < this._wasm.getEventCount(); i++) {
      const { Start, Duration, ReadOrder, Layer, Style, MarginL, MarginR, MarginV, Name, Text, Effect } = this._wasm.getEvent(i);
      events.push({ Start, Duration, ReadOrder, Layer, Style, MarginL, MarginR, MarginV, Name, Text, Effect });
    }
    return events;
  }
  setEvent(event, index) {
    _applyKeys(event, this._wasm.getEvent(index));
  }
  removeEvent(index) {
    this._wasm.removeEvent(index);
  }
  createStyle(style) {
    const alloc = this._wasm.getStyle(this._wasm.allocStyle());
    _applyKeys(style, alloc);
    return alloc;
  }
  getStyles() {
    const styles = [];
    for (let i = 0; i < this._wasm.getStyleCount(); i++) {
      const { Name, FontName, FontSize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding, treat_fontname_as_pattern, Blur, Justify } = this._wasm.getStyle(i);
      styles.push({ Name, FontName, FontSize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding, treat_fontname_as_pattern, Blur, Justify });
    }
    return styles;
  }
  setStyle(style, index) {
    _applyKeys(style, this._wasm.getStyle(index));
  }
  removeStyle(index) {
    this._wasm.removeStyle(index);
  }
  styleOverride(style) {
    this._wasm.styleOverride(this.createStyle(style));
  }
  disableStyleOverride() {
    this._wasm.disableStyleOverride();
  }
  setTrack(content) {
    this._wasm.createTrackMem(content);
    this._subtitleColorSpace = LIBASS_YCBCR_MAP[this._wasm.trackColorSpace];
  }
  freeTrack() {
    this._wasm.removeTrack();
  }
  async setTrackByUrl(url) {
    this.setTrack(await fetchtext(url));
  }
  _checkColorSpace() {
    if (!this._subtitleColorSpace || !this._videoColorSpace)
      return;
    this._gpurender.setColorMatrix(this._subtitleColorSpace, this._videoColorSpace);
  }
  _defaultFont;
  setDefaultFont(fontName) {
    this._defaultFont = fontName.trim().toLowerCase();
    this._wasm.setDefaultFont(this._defaultFont);
  }
  async _log(log) {
    console.debug(log);
    const match = log.match(/JASSUB: fontselect:[^(]+: \(([^,]+), (\d{1,4}), \d\)/);
    if (match && !await this._findAvailableFont(match[1].trim().toLowerCase(), WEIGHT_MAP2[Math.ceil(parseInt(match[2]) / 100) - 1])) {
      await this._findAvailableFont(this._defaultFont);
    }
  }
  async addFonts(fontOrURLs) {
    if (!fontOrURLs.length)
      return;
    const strings = [];
    const uint8s = [];
    for (const fontOrURL of fontOrURLs) {
      if (typeof fontOrURL === "string") {
        strings.push(fontOrURL);
      } else {
        uint8s.push(fontOrURL);
      }
    }
    if (uint8s.length)
      this._allocFonts(uint8s);
    return await Promise.allSettled(strings.map((url) => this._asyncWrite(url)));
  }
  // we don't want to run _findAvailableFont before initial fonts are loaded
  // because it could duplicate fonts
  _loadedInitialFonts = false;
  async _loadInitialFonts(fontOrURLs) {
    await this.addFonts(fontOrURLs);
    this._loadedInitialFonts = true;
  }
  _getFont;
  _availableFonts = {};
  _checkedFonts = /* @__PURE__ */ new Set();
  async _findAvailableFont(fontName, weight) {
    if (!this._loadedInitialFonts)
      return;
    for (const _weight of WEIGHT_MAP2) {
      if (fontName.includes(_weight)) {
        fontName = fontName.replace(_weight, "").trim();
        weight ??= _weight;
        break;
      }
    }
    weight ??= "regular";
    const key = fontName + " " + weight;
    if (this._checkedFonts.has(key))
      return;
    this._checkedFonts.add(key);
    try {
      const font = this._availableFonts[key] ?? this._availableFonts[fontName] ?? await this._queryLocalFont(fontName, weight) ?? await this._queryRemoteFont([key, fontName]);
      if (font)
        return await this.addFonts([font]);
    } catch (e) {
      console.warn("Error querying font", fontName, weight, e);
    }
  }
  queryFonts;
  async _queryLocalFont(fontName, weight) {
    if (!this.queryFonts)
      return;
    return await this._getFont(fontName, weight);
  }
  async _queryRemoteFont(postscriptNames) {
    if (this.queryFonts !== "localandremote")
      return;
    const fontData = await queryRemoteFonts({ postscriptNames });
    if (!fontData.length)
      return;
    const blob = await fontData[0].blob();
    return new Uint8Array(await blob.arrayBuffer());
  }
  async _asyncWrite(font) {
    const res = await _fetch(font);
    this._allocFonts([new Uint8Array(await res.arrayBuffer())]);
  }
  _fontId = 0;
  _allocFonts(uint8s) {
    for (const uint8 of uint8s) {
      const ptr = this._malloc(uint8.byteLength);
      self.HEAPU8RAW.set(uint8, ptr);
      this._wasm.addFont("font-" + this._fontId++, ptr, uint8.byteLength);
    }
    this._wasm.reloadFonts();
  }
  _resizeCanvas(width, height, videoWidth, videoHeight) {
    this._wasm.resizeCanvas(width, height, videoWidth, videoHeight);
    this._gpurender.resizeCanvas(width, height);
  }
  async [finalizer]() {
    await this._ready;
    this._wasm.quitLibrary();
    this._gpurender.destroy();
    this._wasm = null;
    this._gpurender = null;
    this._availableFonts = {};
  }
  _draw(time, repaint = false) {
    if (!this._offCanvas || !this._gpurender)
      return;
    const result = this._wasm.rawRender(time, Number(repaint));
    if (this._wasm.changed === 0 && !repaint)
      return;
    const bitmaps = [];
    for (let image = result, i = 0; i < this._wasm.count; image = image.next, ++i) {
      bitmaps.push({
        bitmap: image.bitmap,
        color: image.color,
        dst_x: image.dst_x,
        dst_y: image.dst_y,
        h: image.h,
        stride: image.stride,
        w: image.w
      });
    }
    this._gpurender.render(bitmaps, self.HEAPU8RAW);
  }
  _setColorSpace(videoColorSpace) {
    if (videoColorSpace === "RGB")
      return;
    this._videoColorSpace = videoColorSpace;
    this._checkColorSpace();
  }
};
if (self.name === "jassub-worker") {
  expose(ASSRenderer);
}
export {
  ASSRenderer
};
