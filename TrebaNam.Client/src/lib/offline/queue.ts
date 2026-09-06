import { api, ApiError } from '$lib/api';
import {
	addWrite,
	patchSnapshot,
	readSnapshot,
	snapshotPaths,
	type PendingWrite
} from '$lib/offline/db';
import { sync } from '$lib/offline/state.svelte';
import type { Item, ItemSuggestion, ShoppingRecord } from '$lib/types';

/** Adresy, ktorych ulozenu odpoved zapisy menia. Rovnake, ake tiahnu obrazovky. */
export const ItemsPath = '/api/items';

export const RecordsPath = '/api/shopping-records';

const SuggestionsPath = '/api/items/suggestions';

/**
 * Meno, ktore veci da telefon. Bez pripojenia musi polozka nejako vzniknut uz tu a rovnake
 * meno potom zabrani tomu, aby z dvakrat odoslaneho zapisu boli dve veci.
 */
export function newID(): string {
	if (typeof crypto.randomUUID === 'function') {
		return crypto.randomUUID();
	}

	// Starsie prehliadace randomUUID mimo zabezpeceneho spojenia nemaju; nahoda je ta ista.
	const bytes = crypto.getRandomValues(new Uint8Array(16));

	bytes[6] = (bytes[6] & 0x0f) | 0x40;
	bytes[8] = (bytes[8] & 0x3f) | 0x80;

	const hex = [...bytes].map((byte) => byte.toString(16).padStart(2, '0')).join('');

	return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

/**
 * Zapis, ktory prezije aj vypadok siete. Kym je pripojenie a rad prazdny, ide rovno na server
 * a nic sa nemeni. Ked siet nie je - alebo ked uz nieco caka a poradie sa nesmie prehodit -
 * ulozi sa do radu a obrazovke sa vrati to, co server podla vsetkeho odpovie. Ulozene odpovede
 * sa pritom prepisu, takze zoznam vyzera hned tak, ako bude vyzerat po odoslani.
 *
 * Odpoved servera sa neobchadza nikdy: ked odmietne (duplicitny nazov, cudzi zoznam), chyba ide
 * na obrazovku tak ako predtym. Do radu sa uklada len to, na co server vobec neodpovedal.
 */
export async function write<T>(
	request: Omit<PendingWrite, 'seq' | 'at'>,
	predict: () => Promise<T>
): Promise<T> {
	if (sync.pending === 0 && navigator.onLine) {
		try {
			const answer = await api<T>(request.path, {
				method: request.method,
				body: request.body === undefined ? undefined : JSON.stringify(request.body)
			});

			sync.online = true;

			return answer;
		} catch (error) {
			if (error instanceof ApiError) throw error;

			sync.online = false;
		}
	}

	await addWrite({ ...request, at: new Date().toISOString() });

	sync.pending += 1;

	return predict();
}

/** Prepise ulozeny zoznam poloziek podla toho, co sa prave zapisalo. */
export function patchItems(change: (items: Item[]) => Item[]) {
	return patchSnapshot<Item[]>(ItemsPath, change, []);
}

export function patchRecords(change: (records: ShoppingRecord[]) => ShoppingRecord[]) {
	return patchSnapshot<ShoppingRecord[]>(RecordsPath, change, []);
}

/** Polozky zo zoznamu, ktore prezili aj vypadok siete - podklad pre predpoved. */
export async function cachedItems(): Promise<Item[]> {
	return (await readSnapshot<Item[]>(ItemsPath)) ?? [];
}

/**
 * Navrhy ma kazdy zoznam vlastne, takze sa prepisuju vsetky ulozene naraz. Hviezdicka patri
 * veci a nie zoznamu, na ktorom prave stojime.
 */
export async function patchSuggestions(
	change: (suggestions: ItemSuggestion[]) => ItemSuggestion[]
) {
	for (const path of await snapshotPaths()) {
		if (path.startsWith(SuggestionsPath)) {
			await patchSnapshot<ItemSuggestion[]>(path, change);
		}
	}
}
