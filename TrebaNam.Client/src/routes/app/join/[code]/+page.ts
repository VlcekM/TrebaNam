import type { PageLoad } from './$types';
import type { HouseholdInvite } from '$lib/types';

export const load: PageLoad = async ({
	fetch,
	params
}): Promise<{ code: string; invite?: HouseholdInvite }> => {
	const res = await fetch(`/api/households/invite/${encodeURIComponent(params.code)}`, {
		credentials: 'include'
	});

	// Neplatny kod nie je chyba appky, len mrtvy odkaz - riesi ho stranka.
	if (res.status === 404) {
		return { code: params.code };
	}

	if (!res.ok) {
		throw new Error(`GET /api/households/invite failed with ${res.status}`);
	}

	return { code: params.code, invite: (await res.json()) as HouseholdInvite };
};
