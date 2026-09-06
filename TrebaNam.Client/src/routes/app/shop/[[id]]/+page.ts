import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import type { Item } from '$lib/types';

/** Nakupuje sa vzdy z jedneho zoznamu; bez adresy plati ten prvy, rovnako ako na /app/list. */
export const load: PageLoad = async ({ parent, fetch, params }) => {
	const { household, lists } = await parent();

	if (!household || lists.length === 0) {
		redirect(302, '/app/household');
	}

	const list = params.id ? lists.find((one) => one.id === params.id) : lists[0];

	if (!list) {
		redirect(302, '/app/list');
	}

	const res = await fetch('/api/items', { credentials: 'include' });

	if (!res.ok) {
		throw new Error(`GET /api/items failed with ${res.status}`);
	}

	const items = (await res.json()) as Item[];

	return { list, items: items.filter((item) => item.listID === list.id) };
};
