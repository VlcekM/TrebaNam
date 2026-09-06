import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import type { ShoppingRecord } from '$lib/types';

export const load: PageLoad = async ({ parent, fetch }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}

	const res = await fetch('/api/shopping-records', { credentials: 'include' });

	if (!res.ok) {
		throw new Error(`GET /api/shopping-records failed with ${res.status}`);
	}

	return { records: (await res.json()) as ShoppingRecord[] };
};
