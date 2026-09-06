import { redirect } from '@sveltejs/kit';
import type { LayoutLoad } from './$types';
import { loadJson, Unauthorized } from '$lib/offline/net';
import type { Household, ShoppingList, User } from '$lib/types';
import { userStore } from '$lib/stores/user';

// /app/* je ciste SPA: ziadny prerender, data sa tahaju z API az v prehliadaci.
export const ssr = false;
export const prerender = false;

// Auth brana: bez platnej cookie posle /api/auth/me 401 a my cloveka poslem na Google.
// Domacnost aj zoznamy sa nacitavaju tu, nie na stranke: prepinac zoznamov aj dialog na
// pridanie veci ich potrebuju na kazdej obrazovke, nielen na tej so zoznamom.
//
// Vsetko ide cez loadJson, takze bez signalu appka ukaze to, co vedela naposledy. Prihlasenie
// je jedina vec, ktora bez servera nejde - 401 je odpoved, nie vypadok, a posiela sa dalej.
export const load: LayoutLoad = async ({
	fetch,
	url
}): Promise<{ user: User; household?: Household; lists: ShoppingList[] }> => {
	let user: User | undefined;

	try {
		user = await loadJson<User>('/api/auth/me', fetch);
	} catch (error) {
		if (!(error instanceof Unauthorized)) throw error;

		// Cielovu adresu nesieme cez prihlasenie, inak by pozvankovy odkaz skoncil na /app.
		const returnUrl = encodeURIComponent(url.pathname + url.search);
		redirect(302, `/api/auth/login?returnUrl=${returnUrl}`);
	}

	if (!user) {
		throw new Error('GET /api/auth/me returned nothing');
	}

	userStore.set(user);

	// Bez domacnosti vracia API 204 s prazdnym telom - z toho je tu undefined.
	const [household, lists] = await Promise.all([
		loadJson<Household>('/api/households/me', fetch),
		loadJson<ShoppingList[]>('/api/lists', fetch)
	]);

	return { user, household, lists: lists ?? [] };
};
