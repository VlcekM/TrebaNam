import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { loadJson } from '$lib/offline/net';
import type { Item, ShoppingRecord } from '$lib/types';

// Bez domacnosti nie je co zhrnat - najprv ju treba zalozit alebo prijat pozvanku.
export const load: PageLoad = async ({ parent, fetch }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}

	// Prehlad zhrna obe obrazovky naraz, takze si ich data tiahne sam; panel z nich nezije.
	const [items, records] = await Promise.all([
		loadJson<Item[]>('/api/items', fetch),
		loadJson<ShoppingRecord[]>('/api/shopping-records', fetch)
	]);

	return { items: items ?? [], records: records ?? [] };
};
