import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		// Absolutne cesty k assetom. SvelteKit inak registruje service worker relativne
		// ("./service-worker.js"), co by na /app/... ukazalo do prazdna.
		paths: { relative: false },

		// Staticky export. Verejne stranky sa predgeneruju do HTML,
		// /app/* bezi ako SPA cez fallback (ma ssr = false a data taha z API).
		adapter: adapter({
			fallback: '200.html',
			strict: false
		}),
		prerender: {
			// Crawler prejde vsetky verejne stranky, na ktore vedie odkaz.
			entries: ['*'],
			handleHttpError: ({ path, message }) => {
				// /api/* obsluhuje backend, nie SvelteKit - pri prerenderi neexistuje.
				if (path.startsWith('/api/')) return;
				throw new Error(message);
			}
		}
	}
};

export default config;
