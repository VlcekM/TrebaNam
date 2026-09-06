/// <reference types="@sveltejs/kit" />
/// <reference lib="webworker" />

import { build, files, version } from '$service-worker';

// SvelteKit registruje tento subor automaticky (len v produkcii, nie v dev serveri).
const worker = self as unknown as ServiceWorkerGlobalScope;

// Nova verzia buildu = nova cache. Stare sa zmazu v 'activate'.
const CACHE = `trebanam-${version}`;

// build = hashovane JS/CSS, files = obsah static/. Oboje je immutable pre danu verziu.
const PRECACHE = [...build, ...files];

// SPA shell z adapter-static. Nie je hashovany, takze ho drzime mimo PRECACHE -
// bez neho by /app/* offline nemalo co zobrazit.
const SHELL = '/200.html';

worker.addEventListener('install', (event) => {
	event.waitUntil(
		caches
			.open(CACHE)
			.then((cache) => cache.addAll([...PRECACHE, SHELL]))
			.then(() => worker.skipWaiting())
	);
});

worker.addEventListener('activate', (event) => {
	event.waitUntil(
		caches
			.keys()
			.then((keys) =>
				Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key)))
			)
			.then(() => worker.clients.claim())
	);
});

worker.addEventListener('fetch', (event) => {
	const url = new URL(event.request.url);

	// Cudzie domeny, non-GET a API nechavame uplne na pokoji - /api/* je stav na serveri
	// a zle zacachovana odpoved by ukazovala neaktualny zoznam alebo cudzie prihlasenie.
	if (event.request.method !== 'GET' || url.origin !== location.origin) return;
	if (url.pathname.startsWith('/api/')) return;

	event.respondWith(
		(async () => {
			const cache = await caches.open(CACHE);

			// Assety buildu su hashovane, takze cache-first je bezpecne a najrychlejsie.
			if (PRECACHE.includes(url.pathname)) {
				const cached = await cache.match(url.pathname);
				if (cached) return cached;
			}

			try {
				const response = await fetch(event.request);
				if (response.ok && response.type === 'basic') {
					cache.put(event.request, response.clone());
				}
				return response;
			} catch (err) {
				// Offline: skusime cache, pri navigacii padneme na SPA shell.
				const cached = await cache.match(event.request);
				if (cached) return cached;
				if (event.request.mode === 'navigate') {
					const shell = await cache.match('/200.html');
					if (shell) return shell;
				}
				throw err;
			}
		})()
	);
});
