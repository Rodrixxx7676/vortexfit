// =============================================
//   Style Gym — Service Worker
//   Estrategia: Cache-first estáticos,
//               Network-first navegación,
//               Offline fallback
// =============================================

const CACHE_VERSION   = 'stylegym-v1';
const OFFLINE_URL     = '/offline.html';

// Activos que se pre-cachean al instalar el SW
const PRE_CACHE = [
  '/',
  '/offline.html',
  '/css/vortex.css',
  '/css/dashboard.css',
  '/js/vortex.js',
  '/images/stylegym_t.png?v=4',
  '/images/favicon.png',
  '/images/icons/icon-192.png',
  '/images/icons/icon-512.png',
  '/manifest.json',
];

// ── INSTALL ────────────────────────────────────
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_VERSION)
      .then(cache => cache.addAll(PRE_CACHE))
      .then(() => self.skipWaiting())
  );
});

// ── ACTIVATE ───────────────────────────────────
// Elimina cachés antiguas al activar nueva versión del SW
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys
          .filter(key => key !== CACHE_VERSION)
          .map(key => caches.delete(key))
      )
    ).then(() => self.clients.claim())
  );
});

// ── PUSH ──────────────────────────────────────
self.addEventListener('push', event => {
  let data = { title: 'Style Gym', body: 'Tienes una clase próximamente.', icon: '/images/icons/icon-192.png', url: '/Dashboard/Clases' };
  try { if (event.data) data = { ...data, ...event.data.json() }; } catch {}
  event.waitUntil(
    self.registration.showNotification(data.title, {
      body: data.body,
      icon: data.icon,
      badge: '/images/icons/icon-192.png',
      data: { url: data.url }
    })
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const target = event.notification.data?.url || '/Dashboard';
  event.waitUntil(clients.openWindow(target));
});

// ── FETCH ─────────────────────────────────────
self.addEventListener('fetch', event => {
  const { request } = event;
  const url = new URL(request.url);

  // Solo manejar peticiones del mismo origen
  if (url.origin !== location.origin) return;

  // No cachear POST, PATCH, DELETE, etc.
  if (request.method !== 'GET') return;

  // No cachear rutas del panel admin ni dashboard (datos dinámicos sensibles)
  const skipCache = ['/Admin', '/Dashboard', '/Account/Login', '/Account/Register'];
  if (skipCache.some(path => url.pathname.startsWith(path))) {
    event.respondWith(
      fetch(request).catch(() => caches.match(OFFLINE_URL))
    );
    return;
  }

  // Activos estáticos (CSS, JS, imágenes, fuentes) → Cache First
  const isStatic = /\.(css|js|png|jpg|jpeg|gif|svg|webp|ico|woff2?|ttf)(\?.*)?$/.test(url.pathname);
  if (isStatic) {
    event.respondWith(
      caches.match(request).then(cached => {
        if (cached) return cached;
        return fetch(request).then(response => {
          if (!response || response.status !== 200) return response;
          const clone = response.clone();
          caches.open(CACHE_VERSION).then(cache => cache.put(request, clone));
          return response;
        });
      })
    );
    return;
  }

  // Navegación HTML → Network First, fallback a offline
  event.respondWith(
    fetch(request)
      .then(response => {
        if (response && response.status === 200) {
          const clone = response.clone();
          caches.open(CACHE_VERSION).then(cache => cache.put(request, clone));
        }
        return response;
      })
      .catch(() =>
        caches.match(request).then(cached => cached || caches.match(OFFLINE_URL))
      )
  );
});
