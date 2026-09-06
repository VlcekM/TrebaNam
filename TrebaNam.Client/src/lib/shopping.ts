import { api } from '$lib/api';
import type { ShoppingRecord } from '$lib/types';

/** Ukonci nakup - z odskrtnutych poloziek spravi zaznam a zo zoznamu ich odoberie. */
export function finishShopping(totalCost?: number) {
	return api<ShoppingRecord>('/api/shopping-records', {
		method: 'POST',
		body: JSON.stringify({ totalCost: totalCost ?? null })
	});
}

/** Zmaze ukonceny nakup aj s jeho riadkami; polozky sa do zoznamu nevracaju. */
export function deleteTrip(id: string) {
	return api<void>(`/api/shopping-records/${id}`, { method: 'DELETE' });
}

/** Doplni alebo opravi sumu za nakup; prazdna hodnota ju odoberie. */
export function setTripTotal(id: string, totalCost?: number) {
	return api<ShoppingRecord>(`/api/shopping-records/${id}/total`, {
		method: 'PUT',
		body: JSON.stringify({ totalCost: totalCost ?? null })
	});
}
