#if USE_DATA_CACHING
const cachePrefix = {{{JSON.stringify(COMPANY_NAME + "-" + PRODUCT_NAME + "-")}}};
const cacheName = {{{JSON.stringify(COMPANY_NAME + "-" + PRODUCT_NAME + "-" + PRODUCT_VERSION)}}} + "-shell-v4";
const contentToCache = [
    "index.html",
    "TemplateData/style.css",
    "TemplateData/pwa-install.js",
    "TemplateData/progress-bar-empty-light.png",
    "TemplateData/progress-bar-empty-dark.png",
    "TemplateData/progress-bar-full-light.png",
    "TemplateData/progress-bar-full-dark.png",
    "TemplateData/icons/mathworld-180.png",
    "TemplateData/icons/mathworld-192.png",
    "TemplateData/icons/mathworld-512.png",
    "TemplateData/icons/mathworld-maskable-512.png",
    "manifest.webmanifest"
];
#endif

self.addEventListener("install", function (event) {
#if USE_DATA_CACHING
    event.waitUntil(caches.open(cacheName).then(function (cache) {
        return cache.addAll(contentToCache);
    }));
#endif
    self.skipWaiting();
});

self.addEventListener("activate", function (event) {
#if USE_DATA_CACHING
    event.waitUntil(caches.keys().then(function (keys) {
        return Promise.all(keys.map(function (key) {
            if (key.startsWith(cachePrefix) && key !== cacheName) {
                return caches.delete(key);
            }
        }));
    }).then(function () {
        return self.clients.claim();
    }));
#else
    event.waitUntil(self.clients.claim());
#endif
});

#if USE_DATA_CACHING
self.addEventListener("fetch", function (event) {
    const request = event.request;
    if (request.method !== "GET") return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    // The release policy must always come from the network.
    if (url.pathname.endsWith("/version.json")) {
        event.respondWith(fetch(request, { cache: "no-store" }));
        return;
    }

    // Prefer the latest Pages shell, with the cached shell as an offline fallback.
    if (request.mode === "navigate") {
        event.respondWith(fetch(request).then(function (response) {
            if (response.ok) {
                const copy = response.clone();
                caches.open(cacheName).then(function (cache) {
                    cache.put("index.html", copy);
                });
            }
            return response;
        }).catch(function () {
            return caches.match("index.html");
        }));
        return;
    }

    event.respondWith(caches.match(request).then(function (cached) {
        if (cached) return cached;

        return fetch(request).then(function (response) {
            if (response.ok && response.type === "basic") {
                const copy = response.clone();
                caches.open(cacheName).then(function (cache) {
                    cache.put(request, copy);
                });
            }
            return response;
        });
    }));
});
#endif
