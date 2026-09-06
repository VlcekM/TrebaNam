import { redirect } from '@sveltejs/kit';
import type { LayoutLoad } from './$types';
import type { Household, User } from '$lib/types';
import { userStore } from '$lib/stores/user';

// /app/* je ciste SPA: ziadny prerender, data sa tahaju z API az v prehliadaci.
export const ssr = false;
export const prerender = false;

/** Bez domacnosti vracia API 204 s prazdnym telom - tu z toho robime undefined. */
async function loadHousehold(fetch: typeof globalThis.fetch): Promise<Household | undefined> {
	const res = await fetch('/api/households/me', { credentials: 'include' });

	if (res.status === 204) {
		return undefined;
	}

	if (!res.ok) {
		throw new Error(`GET /api/households/me failed with ${res.status}`);
	}

	return (await res.json()) as Household;
}

// Auth brana: bez platnej cookie posle /api/auth/me 401 a my cloveka poslem na Google.
// Domacnost sa nacita tu, nie na stranke, lebo z nej zije aj bocny panel v layoute.
export const load: LayoutLoad = async ({
	fetch,
	url
}): Promise<{ user: User; household?: Household }> => {
	const res = await fetch('/api/auth/me', { credentials: 'include' });

	if (res.status === 401) {
		// Cielovu adresu nesieme cez prihlasenie, inak by pozvankovy odkaz skoncil na /app.
		const returnUrl = encodeURIComponent(url.pathname + url.search);
		throw redirect(302, `/api/auth/login?returnUrl=${returnUrl}`);
	}

	if (!res.ok) {
		throw new Error(`GET /api/auth/me failed with ${res.status}`);
	}

	const user = (await res.json()) as User;

	userStore.set(user);

	return { user, household: await loadHousehold(fetch) };
};
