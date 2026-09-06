/**
 * Dve male ulozne priestory v prehliadaci, na ktorych stoji offline rezim.
 *
 * "snapshots" drzi posledne odpovede z API tak, ako prisli - jeden zaznam na adresu. Ked siet
 * nie je, obrazovky sa plnia z nich a appka vyzera presne ako naposledy, nie ako chybova
 * stranka. "outbox" drzi zapisy, ktore este neodisli, v poradi, v akom vznikli.
 *
 * Vsetko je zamerne bez kniznice a vsetko mlci, ked IndexedDB nie je (server pri prerenderi,
 * prehliadac v sukromnom rezime s vypnutym ulozenim): appka potom funguje ako predtym, teda
 * len s pripojenim.
 */
const Name = 'trebanam';

const Version = 1;

const Snapshots = 'snapshots';

const Outbox = 'outbox';

/** Zapis, ktory caka na odoslanie. Nesie cely dopyt, takze ho staci zopakovat tak, ako je. */
export interface PendingWrite {
	/** Poradie v rade; prideluje ho IndexedDB a podla neho sa aj odosiela. */
	seq?: number;
	method: 'POST' | 'PUT' | 'DELETE';
	path: string;
	body?: unknown;
	/** Kedy vznikol - do rozhrania, nie do dopytu. */
	at: string;
}

let opening: Promise<IDBDatabase | undefined> | undefined;

function open(): Promise<IDBDatabase | undefined> {
	if (typeof indexedDB === 'undefined') {
		return Promise.resolve(undefined);
	}

	opening ??= new Promise<IDBDatabase | undefined>((resolve) => {
		const request = indexedDB.open(Name, Version);

		request.onupgradeneeded = () => {
			const db = request.result;

			if (!db.objectStoreNames.contains(Snapshots)) {
				db.createObjectStore(Snapshots);
			}

			if (!db.objectStoreNames.contains(Outbox)) {
				db.createObjectStore(Outbox, { keyPath: 'seq', autoIncrement: true });
			}
		};

		request.onsuccess = () => resolve(request.result);
		// Ulozisko sa otvorit neda (sukromny rezim, plny disk). Nie je to chyba appky.
		request.onerror = () => resolve(undefined);
		request.onblocked = () => resolve(undefined);
	});

	return opening;
}

/** Jedna transakcia, jedna operacia. Zlyhanie sa nikam nehlasi - offline je pomocka, nie podmienka. */
async function run<T>(
	store: string,
	mode: IDBTransactionMode,
	work: (store: IDBObjectStore) => IDBRequest
): Promise<T | undefined> {
	const db = await open();

	if (!db) return undefined;

	return new Promise<T | undefined>((resolve) => {
		try {
			const tx = db.transaction(store, mode);
			const request = work(tx.objectStore(store));

			request.onsuccess = () => resolve(request.result as T);
			request.onerror = () => resolve(undefined);
			tx.onabort = () => resolve(undefined);
		} catch {
			resolve(undefined);
		}
	});
}

export function readSnapshot<T>(path: string): Promise<T | undefined> {
	return run<T>(Snapshots, 'readonly', (store) => store.get(path));
}

export async function writeSnapshot(path: string, data: unknown): Promise<void> {
	// structuredClone padne na tom, co IndexedDB aj tak neulozi; JSON z API taketo veci nenesie.
	await run(Snapshots, 'readwrite', (store) => store.put(data, path));
}

/**
 * Prepise ulozenu odpoved podla toho, co sa prave zapisalo. Ked este ziadna ulozena nie je,
 * zaklada sa na tom, co dostane ako vychodisko - prazdny zoznam pri veciach aj pri nakupoch.
 * Je to o nieco menej, nez vie server, ale je to pravda o tom, co telefon vie: zapis, ktory
 * este neodisiel, ma byt vidiet.
 */
export async function patchSnapshot<T>(
	path: string,
	change: (data: T) => T,
	fallback?: T
): Promise<void> {
	const current = (await readSnapshot<T>(path)) ?? fallback;

	if (current === undefined) return;

	await writeSnapshot(path, change(current));
}

/** Adresy vsetkych ulozenych odpovedi - navrhy ich maju jednu na kazdy zoznam. */
export async function snapshotPaths(): Promise<string[]> {
	return (
		((await run<IDBValidKey[]>(Snapshots, 'readonly', (store) => store.getAllKeys())) as
			string[] | undefined) ?? []
	);
}

export async function addWrite(write: PendingWrite): Promise<void> {
	await run(Outbox, 'readwrite', (store) => store.add(write));
}

export async function listWrites(): Promise<PendingWrite[]> {
	return (await run<PendingWrite[]>(Outbox, 'readonly', (store) => store.getAll())) ?? [];
}

export async function dropWrite(seq: number): Promise<void> {
	await run(Outbox, 'readwrite', (store) => store.delete(seq));
}

export async function countWrites(): Promise<number> {
	return (await run<number>(Outbox, 'readonly', (store) => store.count())) ?? 0;
}

/**
 * Zabudne vsetko. Odhlasenie na spolocnom telefone nema nechat zoznam druhej domacnosti
 * lezat v prehliadaci.
 */
export async function forgetEverything(): Promise<void> {
	await run(Snapshots, 'readwrite', (store) => store.clear());
	await run(Outbox, 'readwrite', (store) => store.clear());
}
