import { api } from '$lib/api';
import { m } from '$lib/paraglide/messages.js';
import type { ShoppingList } from '$lib/types';

/** Rovnake hranice ako v ShoppingListEntity - formular nema pustit dalej, nez API prijme. */
export const ListNameMaxLength = 60;

export const ListNoteMaxLength = 280;

/** Rovnake kody ako ShoppingListColor.All v API a v rovnakom poradi. */
export const LIST_COLORS = ['green', 'blue', 'amber', 'rose', 'violet', 'slate'] as const;

export type ListColor = (typeof LIST_COLORS)[number];

/**
 * Farba je len bodka pri nazve, nie pozadie celej obrazovky: zoznam sa ma dat rozoznat na prvy
 * pohlad, ale citat sa ma dalej rovnako. Odtiene su pevne, aby sedeli vo svetlom aj v tmavom
 * rezime; zelena je rovno akcent appky, nech si tie dve nekonkuruju.
 */
const DOTS: Record<ListColor, string> = {
	green: 'bg-tn-primary',
	blue: 'bg-sky-500',
	amber: 'bg-amber-500',
	rose: 'bg-rose-500',
	violet: 'bg-violet-500',
	slate: 'bg-slate-400'
};

export function listDot(color?: string) {
	return DOTS[color as ListColor] ?? DOTS.green;
}

/** Farba sa vybera klepnutim na farebny kruzok, takze jej meno nesie aspon popis pre citacku. */
const COLOR_NAMES: Record<ListColor, () => string> = {
	green: m.color_green,
	blue: m.color_blue,
	amber: m.color_amber,
	rose: m.color_rose,
	violet: m.color_violet,
	slate: m.color_slate
};

export function listColorName(color: ListColor) {
	return COLOR_NAMES[color]();
}

/** Polia, ktore o zozname urcuje clovek; rovnake pri zalozeni aj pri uprave. */
export interface ListFields {
	name: string;
	color: string;
	note?: string;
}

export function createList(fields: ListFields) {
	return api<ShoppingList>('/api/lists', {
		method: 'POST',
		body: JSON.stringify(fields)
	});
}

export function updateList(id: string, fields: ListFields) {
	return api<ShoppingList>(`/api/lists/${id}`, {
		method: 'PUT',
		body: JSON.stringify(fields)
	});
}

/** Zmaze zoznam aj s tym, co na nom ostalo. Posledny zoznam API zmazat nedovoli. */
export function deleteList(id: string) {
	return api<void>(`/api/lists/${id}`, { method: 'DELETE' });
}
