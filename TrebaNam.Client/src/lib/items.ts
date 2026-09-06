import { api } from '$lib/api';
import type { Item, ItemSuggestion } from '$lib/types';

/** Rovnake hranice ako v ItemEntity - formular nema pustit dalej, nez API prijme. */
export const ItemNameMaxLength = 120;

export const ItemQuantityMaxLength = 24;

export const ItemNoteMaxLength = 280;

/** Polia, ktore o polozke urcuje clovek; rovnake pri zalozeni aj pri uprave. */
export interface ItemFields {
	name: string;
	quantity?: string;
	category: string;
	note?: string;
}

export function createItem(fields: ItemFields) {
	return api<Item>('/api/items', {
		method: 'POST',
		body: JSON.stringify(fields)
	});
}

export function updateItem(id: string, fields: ItemFields) {
	return api<Item>(`/api/items/${id}`, {
		method: 'PUT',
		body: JSON.stringify(fields)
	});
}

export function deleteItem(id: string) {
	return api<void>(`/api/items/${id}`, { method: 'DELETE' });
}

export function setItemChecked(id: string, isChecked: boolean) {
	return api<Item>(`/api/items/${id}/checked`, {
		method: 'PUT',
		body: JSON.stringify({ isChecked })
	});
}

/** Co uz domacnost kupovala, od najcastejsieho a bez toho, co prave je na zozname. */
export function fetchItemSuggestions() {
	return api<ItemSuggestion[]>('/api/items/suggestions');
}

/** Zapise, ze sa kupila len cast; prazdna hodnota ciastocny nakup zrusi. */
export function setItemBoughtQuantity(id: string, quantity?: string) {
	return api<Item>(`/api/items/${id}/bought`, {
		method: 'PUT',
		body: JSON.stringify({ quantity: quantity ?? null })
	});
}
