import { api } from '$lib/api';
import { insertAlphabetically } from '$lib/categories';
import type { Household } from '$lib/types';

/** Odkaz, ktory sa posiela druhemu clenovi domacnosti. */
export function inviteLink(code: string, origin: string = location.origin) {
	return `${origin}/app/join/${code}`;
}

/** Nazov prveho zoznamu ide so zalozenim - je to text pre cloveka a API o jeho jazyku nevie. */
export function createHousehold(name: string, listName: string) {
	return api<Household>('/api/households', {
		method: 'POST',
		body: JSON.stringify({ name, listName })
	});
}

export function renameHousehold(name: string) {
	return api<Household>('/api/households/me', {
		method: 'PUT',
		body: JSON.stringify({ name })
	});
}

/** Skupiny v novom poradi, po ID. Server si poradie doplni na uplne, takze staci poslat to nove. */
export function setCategoryOrder(order: string[]) {
	return api<Household>('/api/households/me/categories', {
		method: 'PUT',
		body: JSON.stringify({ order })
	});
}

/**
 * Vlastna skupina domacnosti. Kod si k nej odvodi server, tu ide len nazov. Server ju da na
 * koniec, lebo nazvy zakladnych skupin pozna len klient - tak ju odtial rovno presunieme tam,
 * kam patri podla abecedy. Druhy zapis je zvlast: skupina uz existuje, aj keby poradie nepreslo.
 */
export async function createCategory(name: string) {
	const household = await api<Household>('/api/households/me/categories', {
		method: 'POST',
		body: JSON.stringify({ name })
	});

	const added = household.categories.at(-1);

	if (!added) return household;

	const order = insertAlphabetically(household.categories, added);

	if (order.every((category, index) => category.id === household.categories[index].id)) {
		return household;
	}

	return setCategoryOrder(order.map((category) => category.id));
}

/** Premenovanie skupiny. Kod ostava, takze polozky v nej nikam neprechadzaju. */
export function renameCategory(id: string, name: string) {
	return api<Household>(`/api/households/me/categories/${id}`, {
		method: 'PUT',
		body: JSON.stringify({ name })
	});
}

/** Zrusenie skupiny. Polozky v nej prejdu do "ostatne", nemazu sa. */
export function deleteCategory(id: string) {
	return api<Household>(`/api/households/me/categories/${id}`, { method: 'DELETE' });
}

/** Odchod z domacnosti. Ked odchadza posledny, odchadza s nim aj zoznam a nakupy. */
export function leaveHousehold() {
	return api<void>('/api/households/me/leave', { method: 'POST' });
}

export function removeMember(id: string) {
	return api<Household>(`/api/households/members/${id}`, { method: 'DELETE' });
}

export function joinHousehold(code: string) {
	return api<Household>(`/api/households/invite/${encodeURIComponent(code)}/join`, {
		method: 'POST'
	});
}
