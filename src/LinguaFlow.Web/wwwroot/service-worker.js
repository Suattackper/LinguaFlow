// Development host: keep network changes immediately visible.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
self.addEventListener('message', event => event.ports[0]?.postMessage({ event: 'error', message: 'Offline downloads are available in the published app.' }));
