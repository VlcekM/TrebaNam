import { get } from 'svelte/store';
import { loadJson } from '$lib/offline/net';
import { cachedItems, newID, patchItems, patchSuggestions, write } from '$lib/offline/queue';
import { userStore } from '$lib/stores/user';
import type { Item, ItemSuggestion } from '$lib/types';

/** Rovnake hranice ako v ItemEntity - formular nema pustit dalej, nez API prijme. */
export const ItemNameMaxLength = 120;

export const ItemQuantityMaxLength = 24;

export const ItemNoteMaxLength = 280;

/** Polia, ktore o polozke urcuje clovek; rovnake pri zalozeni aj pri uprave. */
export interface ItemFields {
	/** Na ktory zoznam patri. Pri uprave sa tym da presunut na iny. */
	listID: string;
	name: string;
	quantity?: string;
	category: string;
	note?: string;
}

/**
 * To iste, co robi ItemName.Display na serveri: male zaciatocne pismeno sa zvacsi, zvysok sa
 * necha tak. Bez pripojenia riadok pise telefon a ma citat rovnako, ako bude citat potom.
 */
function display(name: string) {
	const clean = name.trim();

	return clean ? clean[0].toLocaleUpperCase() + clean.slice(1) : clean;
}

function clean(fields: ItemFields) {
	return {
		listID: fields.listID,
		name: display(fields.name),
		quantity: fields.quantity?.trim() || undefined,
		category: fields.category,
		note: fields.note?.trim() || undefined
	};
}

/**
 * Prida vec na zoznam. Meno jej dava telefon, nie server: bez pripojenia musi polozka vzniknut
 * uz tu a vdaka rovnakemu menu z nej ani pri opakovanom odoslani nebudu dve.
 */
export function createItem(fields: ItemFields) {
	const values = clean(fields);

	const item: Item = {
		id: newID(),
		...values,
		isChecked: false,
		addedByUserID: get(userStore)?.id ?? '',
		createdAt: new Date().toISOString()
	};

	return write<Item>(
		{ method: 'POST', path: '/api/items', body: { id: item.id, ...values } },
		async () => {
			await patchItems((items) => [...items, item]);

			return item;
		}
	);
}

export function updateItem(id: string, fields: ItemFields) {
	const values = clean(fields);

	return write<Item>({ method: 'PUT', path: `/api/items/${id}`, body: values }, async () => {
		const changed = await patch(id, (item) => ({ ...item, ...values }));

		return changed;
	});
}

export function deleteItem(id: string) {
	return write<void>({ method: 'DELETE', path: `/api/items/${id}` }, async () => {
		await patchItems((items) => items.filter((item) => item.id !== id));
	});
}

export function setItemChecked(id: string, isChecked: boolean) {
	return write<Item>({ method: 'PUT', path: `/api/items/${id}/checked`, body: { isChecked } }, () =>
		// Cela polozka a jej odnesena cast su dva rozne stavy, presne ako na serveri.
		patch(id, (item) => ({
			...item,
			isChecked,
			boughtQuantity: isChecked ? undefined : item.boughtQuantity
		}))
	);
}

/** Zapise, ze sa kupila len cast; prazdna hodnota ciastocny nakup zrusi. */
export function setItemBoughtQuantity(id: string, quantity?: string) {
	return write<Item>(
		{ method: 'PUT', path: `/api/items/${id}/bought`, body: { quantity: quantity ?? null } },
		() =>
			patch(id, (item) => ({
				...item,
				boughtQuantity: quantity,
				isChecked: quantity ? false : item.isChecked
			}))
	);
}

/** Prepise jednu polozku v ulozenom zozname a vrati ju tak, ako po zapise vyzera. */
async function patch(id: string, change: (item: Item) => Item): Promise<Item> {
	const before = (await cachedItems()).find((item) => item.id === id);
	const after = before ? change(before) : undefined;

	await patchItems((items) => items.map((item) => (item.id === id ? change(item) : item)));

	// Polozka, ktoru sme nikdy nemali ulozenu, sa upravit nedala - obrazovka aj tak cita zoznam.
	return after ?? ({ id } as Item);
}

/**
 * Co uz domacnost kupovala, od najcastejsieho. To, co uz na tom zozname stoji, sa vynechava -
 * na jednom zozname dvakrat nie, na inom zozname domacnosti pokojne. Bez pripojenia plati to,
 * co naposledy prislo: naseptavac je pomocka a stara pomoc je lepsia nez ziadna.
 */
export async function fetchItemSuggestions(listID: string) {
	const path = `/api/items/suggestions?listID=${encodeURIComponent(listID)}`;

	return (await loadJson<ItemSuggestion[]>(path, fetch)) ?? [];
}

/** Prida alebo zoberie hviezdicku veci, ktoru domacnost kupuje stale dokola. */
export function setItemFavourite(fields: {
	name: string;
	quantity?: string;
	category: string;
	isFavourite: boolean;
}) {
	return write<void>({ method: 'PUT', path: '/api/items/favourites', body: fields }, async () => {
		await patchSuggestions((suggestions) =>
			suggestions.map((suggestion) =>
				suggestion.name === fields.name
					? { ...suggestion, isFavourite: fields.isFavourite }
					: suggestion
			)
		);
	});
}
