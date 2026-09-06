import { readSnapshot, writeSnapshot } from '$lib/offline/db';
import { sync } from '$lib/offline/state.svelte';

/** Odpoved 401 znamena, ze server odpovedal - to nie je vypadok siete a rieti sa prihlasenim. */
export class Unauthorized extends Error {
	constructor(public path: string) {
		super(`GET ${path} answered 401`);
	}
}

/**
 * Nacitanie, ktore prezije aj vypadok siete. Kazda odpoved sa odklada do prehliadaca a ked
 * sa dopyt nepodari, vrati sa ta ulozena - appka teda ukaze to, co naposledy vedela, namiesto
 * chybovej stranky. Bez ulozenej odpovede sa chyba posiela dalej, lebo vtedy naozaj niet co
 * ukazat.
 *
 * Prazdne telo (204 pri domacnosti) je platna odpoved a uklada sa tiez - "domacnost nie je"
 * je informacia rovnako ako domacnost sama.
 */
export async function loadJson<T>(
	path: string,
	fetch: typeof globalThis.fetch
): Promise<T | undefined> {
	try {
		const res = await fetch(path, { credentials: 'include' });

		// Server odpoveda, takze sme online aj vtedy, ked odpoved nie je ta, ktoru sme chceli.
		sync.online = true;

		if (res.status === 401) {
			throw new Unauthorized(path);
		}

		// Chyba na strane servera je pre telefon to iste ako vypadok - stara odpoved je lepsia
		// nez ziadna. Ostatne stavy su odpoved o nasom dopyte a tie sa neobchadzaju.
		if (!res.ok && res.status < 500) {
			throw new Error(`GET ${path} failed with ${res.status}`);
		}

		if (!res.ok) {
			return await fallback<T>(path, new Error(`GET ${path} failed with ${res.status}`), true);
		}

		const text = await res.text();
		const data = (text ? JSON.parse(text) : null) as T | null;

		await writeSnapshot(path, data);

		return data ?? undefined;
	} catch (error) {
		if (error instanceof Unauthorized) throw error;

		sync.online = false;

		return await fallback<T>(path, error, false);
	}
}

/**
 * Ulozena odpoved namiesto tej, ktora neprisla. Ked ziadna nie je, rozhoduje, ci server vobec
 * odpovedal: jeho chyba sa posiela dalej, vypadok siete nie - bez signalu je prazdna obrazovka
 * blizsie k pravde nez chybova hlaska, appka o tej adrese jednoducho nic nevie.
 */
async function fallback<T>(
	path: string,
	error: unknown,
	answered: boolean
): Promise<T | undefined> {
	const cached = await readSnapshot<T | null>(path);

	if (cached === undefined) {
		if (answered) throw error;

		return undefined;
	}

	return cached ?? undefined;
}
