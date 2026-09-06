import { redirect } from '@sveltejs/kit';
import type { LayoutLoad } from './$types';
import type { Household, ShoppingList, User } from '$lib/types';
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

/** Zoznamy domacnosti. Bez domacnosti nie su ziadne, takze API vracia prazdne pole. */
async function loadLists(fetch: typeof globalThis.fetch): Promise<ShoppingList[]> {
	const res = await fetch('/api/lists', { credentials: 'include' });

	if (!res.ok) {
		throw new Error(`GET /api/lists failed with ${res.status}`);
	}

	return (await res.json()) as ShoppingList[];
}

// Auth brana: bez platnej cookie posle /api/auth/me 401 a my cloveka poslem na Google.
// Domacnost aj zoznamy sa nacitavaju tu, nie na stranke: prepinac zoznamov aj dialog na
// pridanie veci ich potrebuju na kazdej obrazovke, nielen na tej so zoznamom.
export const load: LayoutLoad = async ({
	fetch,
	url
}): Promise<{ user: User; household?: Household; lists: ShoppingList[] }> => {
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

	const [household, lists] = await Promise.all([loadHousehold(fetch), loadLists(fetch)]);

	return { user, household, lists };
};
