import { api } from '$lib/api';
import type { Household } from '$lib/types';

/** Odkaz, ktory sa posiela druhemu clenovi domacnosti. */
export function inviteLink(code: string, origin: string = location.origin) {
	return `${origin}/app/join/${code}`;
}

export function createHousehold(name: string) {
	return api<Household>('/api/households', {
		method: 'POST',
		body: JSON.stringify({ name })
	});
}

export function joinHousehold(code: string) {
	return api<Household>(`/api/households/invite/${encodeURIComponent(code)}/join`, {
		method: 'POST'
	});
}
