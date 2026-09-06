import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';

// Bez domacnosti nie je co kupovat - najprv ju treba zalozit alebo prijat pozvanku.
export const load: PageLoad = async ({ parent }) => {
	const { household } = await parent();

	if (!household) {
		redirect(302, '/app/household');
	}
};
