import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { loadJson } from '$lib/offline/net';
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

	const items = (await loadJson<Item[]>('/api/items', fetch)) ?? [];

	return { list, items: items.filter((item) => item.listID === list.id) };
};
