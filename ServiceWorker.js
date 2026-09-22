const cacheName = "SproutFarm";
const contentToCache = [
    "Build/SproutFarm.loader.js",
    "Build/SproutFarm.framework.js.unityweb",
    "Build/SproutFarm.data.unityweb",
    "Build/SproutFarm.wasm.unityweb",
    "TemplateData/style.css"

];

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');
    // Take over from an older worker right away so a new build is picked up on the next load.
    self.skipWaiting();

    e.waitUntil((async function () {
      const cache = await caches.open(cacheName);
      console.log('[Service Worker] Caching all: app shell and content');
      await cache.addAll(contentToCache);
    })());
});

self.addEventListener('activate', function (e) {
    // Drop caches left by earlier workers (they would keep serving the old build).
    e.waitUntil((async function () {
      for (const key of await caches.keys()) {
        if (key !== cacheName) {
          await caches.delete(key);
        }
      }
      await self.clients.claim();
    })());
});

// Network first so deploys show up immediately; the cache is only an offline fallback.
self.addEventListener('fetch', function (e) {
    e.respondWith((async function () {
      try {
        const response = await fetch(e.request);
        // Leaderboard responses must stay live, so only static files are cached.
        if (e.request.method === 'GET' && response.status === 200 && !new URL(e.request.url).pathname.startsWith('/api/')) {
          const cache = await caches.open(cacheName);
          cache.put(e.request, response.clone());
        }
        return response;
      } catch (error) {
        const cached = await caches.match(e.request);
        if (cached) {
          return cached;
        }
        throw error;
      }
    })());
});
