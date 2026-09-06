import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import type { Item, ShoppingRecord } from '$lib/types';

// Bez domacnosti nie je co zhrnat - najprv ju treba zalozit alebo prijat pozvanku.
export const load: PageLoad = async ({ parent, fetch }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}

	// Prehlad zhrna obe obrazovky naraz, takze si ich data tiahne sam; panel z nich nezije.
	const [items, records] = await Promise.all([
		fetch('/api/items', { credentials: 'include' }),
		fetch('/api/shopping-records', { credentials: 'include' })
	]);

	if (!items.ok) {
		throw new Error(`GET /api/items failed with ${items.status}`);
	}

	if (!records.ok) {
		throw new Error(`GET /api/shopping-records failed with ${records.status}`);
	}

	return {
		items: (await items.json()) as Item[],
		records: (await records.json()) as ShoppingRecord[]
	};
};
