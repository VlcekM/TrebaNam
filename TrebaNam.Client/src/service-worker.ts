/// <reference types="@sveltejs/kit" />
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />

import { base, build, files, prerendered, version } from '$service-worker';

/**
 * Appka sa otvara na plochu telefonu a v obchode zvykne byt signal najhorsi, takze sa musi
 * spustit aj bez neho. Service worker si preto odlozi cely shell - skripty, styly, pisma,
 * ikony - a offline z nej appku poskladame.
 *
 * Data cez neho nechodia: /api/* sa neuklada vobec. Odpovede si odklada samotna appka
 * (src/lib/offline), lebo len ona vie, co s nimi robit, ked sa medzitym nieco zmenilo.
 */
const sw = self as unknown as ServiceWorkerGlobalScope;

// Verzia je hash buildu, takze kazde nasadenie ma vlastnu cache a stara sa cela zahodi.
const CacheName = `trebanam-${version}`;

/** Prazdna stranka SPA. Kazda adresa pod /app sa kresli az v prehliadaci, takze staci jedna. */
const Shell = `${base}/200.html`;

const Precache = [...build, ...files, ...prerendered];

const Precached = new Set(Precache);

sw.addEventListener('install', (event) => {
	event.waitUntil(
		(async () => {
			const cache = await caches.open(CacheName);

			await cache.addAll(Precache);

			// Shell nie je medzi assetmi - vyroba ho az adapter. Bez neho by offline start
			// skoncil na chybovej stranke prehliadaca, ale nie je dovod kvoli nemu padnut.
			try {
				await cache.add(Shell);
			} catch {
				// v developmente ziadny shell nie je
			}

			// Nova verzia ma platit hned. Navigacia chodi na siet ako prva, takze cerstve HTML
			// si vzdy pyta assety, ktore k nemu patria.
			await sw.skipWaiting();
		})()
	);
});

sw.addEventListener('activate', (event) => {
	event.waitUntil(
		(async () => {
			for (const key of await caches.keys()) {
				if (key !== CacheName) await caches.delete(key);
			}

			await sw.clients.claim();
		})()
	);
});

sw.addEventListener('fetch', (event) => {
	const { request } = event;

	if (request.method !== 'GET') return;

	const url = new URL(request.url);

	// Cudzie adresy a API si riesi appka sama - zapisy maju vlastny rad a data vlastne ulozisko.
	if (url.origin !== location.origin || url.pathname.startsWith('/api')) return;

	event.respondWith(answer(request, url));
});

async function answer(request: Request, url: URL): Promise<Response> {
	const cache = await caches.open(CacheName);

	// Assety maju hash v nazve, takze ulozena kopia je vzdy ta spravna a siet uz netreba.
	if (Precached.has(url.pathname)) {
		const hit = await cache.match(url.pathname);

		if (hit) return hit;
	}

	try {
		const fresh = await fetch(request);

		// Podarene odpovede si drzime; 404 ani presmerovanie ukladat netreba.
		if (fresh.ok && fresh.type === 'basic') {
			cache.put(request, fresh.clone());
		}

		return fresh;
	} catch (error) {
		const hit = await cache.match(request);

		if (hit) return hit;

		// Otvorenie appky bez signalu: shell vie zvysok poskladat z ulozenych odpovedi.
		if (request.mode === 'navigate') {
			const shell = await cache.match(Shell);

			if (shell) return shell;
		}

		throw error;
	}
}
