import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { loadJson } from '$lib/offline/net';
import type { ShoppingRecord } from '$lib/types';

export const load: PageLoad = async ({ parent, fetch }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}

	return { records: (await loadJson<ShoppingRecord[]>('/api/shopping-records', fetch)) ?? [] };
};
