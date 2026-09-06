import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import type { Item } from '$lib/types';

export const load: PageLoad = async ({ parent, fetch }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}

	const res = await fetch('/api/items', { credentials: 'include' });

	if (!res.ok) {
		throw new Error(`GET /api/items failed with ${res.status}`);
	}

	return { items: (await res.json()) as Item[] };
};
