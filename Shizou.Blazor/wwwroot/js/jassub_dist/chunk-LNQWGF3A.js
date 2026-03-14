// node_modules/abslink/src/types.js
var WireValueType;
(function(WireValueType2) {
  WireValueType2["RAW"] = "RAW";
  WireValueType2["PROXY"] = "PROXY";
  WireValueType2["THROW"] = "THROW";
  WireValueType2["HANDLER"] = "HANDLER";
})(WireValueType || (WireValueType = {}));
var MessageType;
(function(MessageType2) {
  MessageType2["GET"] = "GET";
  MessageType2["SET"] = "SET";
  MessageType2["APPLY"] = "APPLY";
  MessageType2["CONSTRUCT"] = "CONSTRUCT";
  MessageType2["RELEASE"] = "RELEASE";
})(MessageType || (MessageType = {}));

// node_modules/abslink/src/abslink.js
var proxyMarker = /* @__PURE__ */ Symbol("Abslink.proxy");
var releaseProxy = /* @__PURE__ */ Symbol("Abslink.releaseProxy");
var finalizer = /* @__PURE__ */ Symbol("Abslink.finalizer");
var throwMarker = /* @__PURE__ */ Symbol("Abslink.thrown");
var isObject = (val) => typeof val === "object" && val !== null || typeof val === "function";
var proxyTransferHandler = {
  canHandle: (val) => isObject(val) && proxyMarker in val,
  serialize(obj, ep) {
    const markerID = obj[proxyMarker];
    expose(obj, ep, markerID);
    return markerID;
  },
  deserialize(markerID, ep) {
    return wrap(ep, void 0, markerID);
  }
};
function closeEndPoint(endpoint) {
  if ("close" in endpoint && typeof endpoint.close === "function")
    endpoint.close();
}
var throwTransferHandler = {
  canHandle: (value) => isObject(value) && throwMarker in value,
  serialize({ value }) {
    let serialized;
    if (value instanceof Error) {
      serialized = {
        isError: true,
        value: {
          message: value.message,
          name: value.name,
          stack: value.stack
        }
      };
    } else {
      serialized = { isError: false, value };
    }
    return serialized;
  },
  deserialize(serialized) {
    if (serialized.isError) {
      throw Object.assign(new Error(serialized.value.message), serialized.value);
    }
    throw serialized.value;
  }
};
var transferHandlers = /* @__PURE__ */ new Map([
  ["proxy", proxyTransferHandler],
  ["throw", throwTransferHandler]
]);
function filterPath(path, obj) {
  let parent = obj;
  const parentPath = path.slice(0, -1);
  for (const segment of parentPath) {
    if (Object.prototype.hasOwnProperty.call(parent, segment)) {
      parent = parent[segment];
    }
  }
  const lastSegment = path[path.length - 1];
  const RawValue = lastSegment ? parent[lastSegment] : parent;
  return { parent, RawValue, lastSegment };
}
function expose(obj, ep, rootMarkerID) {
  ep.on("message", function callback(data) {
    if (!data)
      return;
    const { id, type, path, markerID } = {
      path: [],
      ...data
    };
    if (markerID !== rootMarkerID)
      return;
    const argumentList = (data.argumentList ?? []).map((v) => fromWireValue(v, ep));
    let returnValue;
    try {
      const { parent, RawValue, lastSegment } = filterPath(path, obj);
      switch (type) {
        case MessageType.GET:
          returnValue = RawValue;
          break;
        case MessageType.SET:
          parent[lastSegment] = fromWireValue(data.value, ep);
          returnValue = true;
          break;
        case MessageType.APPLY:
          returnValue = RawValue.apply(parent, argumentList);
          break;
        case MessageType.CONSTRUCT:
          {
            const value = new RawValue(...argumentList);
            returnValue = proxy(value);
          }
          break;
        case MessageType.RELEASE:
          returnValue = void 0;
          break;
        default:
          return;
      }
    } catch (value) {
      returnValue = { value, [throwMarker]: 0 };
    }
    Promise.resolve(returnValue).catch((value) => {
      return { value, [throwMarker]: 0 };
    }).then((returnValue2) => {
      const wireValue = toWireValue(returnValue2, ep);
      ep.postMessage({ ...wireValue, id, markerID: rootMarkerID });
      if (type === MessageType.RELEASE) {
        ep.off("message", callback);
        if (finalizer in obj && typeof obj[finalizer] === "function") {
          obj[finalizer]();
        }
      }
    }).catch((_) => {
      const wireValue = toWireValue({
        value: new TypeError("Unserializable return value"),
        [throwMarker]: 0
      }, ep);
      ep.postMessage({ ...wireValue, id, markerID: rootMarkerID });
    });
  });
  return obj;
}
function wrap(ep, target, rootMarkerID) {
  const pendingListeners = /* @__PURE__ */ new Map();
  ep.on("message", (data) => {
    if (!data?.id) {
      return;
    }
    const resolver = pendingListeners.get(data.id);
    if (!resolver) {
      return;
    }
    try {
      resolver(data);
    } finally {
      pendingListeners.delete(data.id);
    }
  });
  return createProxy({ endpoint: ep, pendingListeners, nextRequestId: 1 }, [], target, rootMarkerID);
}
function throwIfProxyReleased(isReleased) {
  if (isReleased) {
    throw new Error("Proxy has been released and is not useable");
  }
}
async function releaseEndpoint(epWithPendingListeners) {
  await requestResponseMessage(epWithPendingListeners, { type: MessageType.RELEASE });
  closeEndPoint(epWithPendingListeners.endpoint);
}
var proxyCounter = /* @__PURE__ */ new WeakMap();
var proxyFinalizers = "FinalizationRegistry" in globalThis && new FinalizationRegistry((epWithPendingListeners) => {
  const newCount = (proxyCounter.get(epWithPendingListeners) ?? 0) - 1;
  proxyCounter.set(epWithPendingListeners, newCount);
  if (newCount === 0) {
    releaseEndpoint(epWithPendingListeners).finally(() => {
      epWithPendingListeners.pendingListeners.clear();
    });
  }
});
function registerProxy(proxy2, epWithPendingListeners) {
  const newCount = (proxyCounter.get(epWithPendingListeners) ?? 0) + 1;
  proxyCounter.set(epWithPendingListeners, newCount);
  if (proxyFinalizers) {
    proxyFinalizers.register(proxy2, epWithPendingListeners, proxy2);
  }
}
function unregisterProxy(proxy2) {
  if (proxyFinalizers) {
    proxyFinalizers.unregister(proxy2);
  }
}
function createProxy(epWithPendingListeners, path = [], target = function() {
}, rootMarkerID) {
  let isProxyReleased = false;
  const propProxyCache = /* @__PURE__ */ new Map();
  const proxy2 = new Proxy(target, {
    get(_target, prop) {
      throwIfProxyReleased(isProxyReleased);
      if (prop === releaseProxy) {
        return async () => {
          for (const subProxy of propProxyCache.values()) {
            subProxy[releaseProxy]();
          }
          propProxyCache.clear();
          unregisterProxy(proxy2);
          releaseEndpoint(epWithPendingListeners).finally(() => {
            epWithPendingListeners.pendingListeners.clear();
          });
          isProxyReleased = true;
        };
      }
      if (prop === "then") {
        if (path.length === 0) {
          return { then: () => proxy2 };
        }
        const r = requestResponseMessage(epWithPendingListeners, {
          type: MessageType.GET,
          markerID: rootMarkerID,
          path: path.map((p) => p.toString())
        }).then((v) => fromWireValue(v, epWithPendingListeners.endpoint));
        return r.then.bind(r);
      }
      const cachedProxy = propProxyCache.get(prop);
      if (cachedProxy) {
        return cachedProxy;
      }
      const propProxy = createProxy(epWithPendingListeners, [...path, prop], void 0, rootMarkerID);
      propProxyCache.set(prop, propProxy);
      return propProxy;
    },
    set(_target, prop, rawValue) {
      throwIfProxyReleased(isProxyReleased);
      const value = toWireValue(rawValue, epWithPendingListeners.endpoint);
      return requestResponseMessage(epWithPendingListeners, {
        type: MessageType.SET,
        markerID: rootMarkerID,
        path: [...path, prop].map((p) => p.toString()),
        value
      }).then((v) => fromWireValue(v, epWithPendingListeners.endpoint));
    },
    apply(_target, _thisArg, rawArgumentList) {
      throwIfProxyReleased(isProxyReleased);
      const last = path[path.length - 1];
      if (last === "bind") {
        return createProxy(epWithPendingListeners, path.slice(0, -1), void 0, rootMarkerID);
      }
      const argumentList = processArguments(rawArgumentList, epWithPendingListeners);
      return requestResponseMessage(epWithPendingListeners, {
        type: MessageType.APPLY,
        markerID: rootMarkerID,
        path: path.map((p) => p.toString()),
        argumentList
      }).then((v) => fromWireValue(v, epWithPendingListeners.endpoint));
    },
    construct(_target, rawArgumentList) {
      throwIfProxyReleased(isProxyReleased);
      const argumentList = processArguments(rawArgumentList, epWithPendingListeners);
      return requestResponseMessage(epWithPendingListeners, {
        type: MessageType.CONSTRUCT,
        markerID: rootMarkerID,
        path: path.map((p) => p.toString()),
        argumentList
      }).then((v) => fromWireValue(v, epWithPendingListeners.endpoint));
    }
  });
  registerProxy(proxy2, epWithPendingListeners);
  return proxy2;
}
function processArguments(argumentList, epWithPendingListeners) {
  return argumentList.map((v) => toWireValue(v, epWithPendingListeners.endpoint));
}
function proxy(obj) {
  return Object.assign(obj, { [proxyMarker]: randomId() });
}
function toWireValue(value, ep) {
  for (const [name, handler] of transferHandlers) {
    if (handler.canHandle(value)) {
      const serializedValue = handler.serialize(value, ep);
      return {
        type: WireValueType.HANDLER,
        name,
        value: serializedValue
      };
    }
  }
  return {
    type: WireValueType.RAW,
    value
  };
}
function fromWireValue(value, ep) {
  switch (value.type) {
    case WireValueType.HANDLER:
      return transferHandlers.get(value.name).deserialize(value.value, ep);
    case WireValueType.RAW:
      return value.value;
  }
}
function requestResponseMessage(ep, msg) {
  return new Promise((resolve) => {
    const id = randomId();
    ep.pendingListeners.set(id, resolve);
    ep.endpoint.postMessage({ id, ...msg });
  });
}
function randomId() {
  return Math.trunc(Math.random() * Number.MAX_SAFE_INTEGER).toString();
}

// node_modules/abslink/adapters/w3c.js
function createWrapper(channel, messageable) {
  const listeners = /* @__PURE__ */ new WeakMap();
  return {
    on(event, listener) {
      const unwrapped = (event2) => listener(event2.data);
      if ("addEventListener" in channel) {
        channel.addEventListener(event, unwrapped);
      } else if ("addListener" in channel) {
        channel.addListener(event, unwrapped);
      } else {
        channel.on(event, unwrapped);
      }
      listeners.set(listener, unwrapped);
    },
    off(event, listener) {
      const unwrapped = listeners.get(listener);
      if ("removeEventListener" in channel) {
        channel.removeEventListener(event, unwrapped);
      } else if ("removeListener" in channel) {
        channel.removeListener(event, unwrapped);
      } else {
        channel.off(event, unwrapped);
      }
      listeners.delete(listener);
    },
    postMessage(message) {
      messageable.postMessage(message);
    },
    [finalizer]: () => {
      channel.terminate?.();
      messageable.terminate?.();
    }
  };
}
function wrap2(channel, messageable = channel) {
  return wrap(createWrapper(channel, messageable));
}
function expose2(obj, channel = self, messageable = channel) {
  return expose(obj, createWrapper(channel, messageable));
}

export {
  releaseProxy,
  finalizer,
  proxy,
  wrap2 as wrap,
  expose2 as expose
};
//# sourceMappingURL=chunk-LNQWGF3A.js.map
