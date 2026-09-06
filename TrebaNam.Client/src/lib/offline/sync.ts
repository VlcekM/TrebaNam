import { invalidateAll } from '$app/navigation';
import { api, ApiError } from '$lib/api';
import { countWrites, dropWrite, listWrites } from '$lib/offline/db';
import { sync } from '$lib/offline/state.svelte';

/** Ako casto sa appka sama pokusi rad odoslat, kym v nom nieco stoji. */
const RetryEvery = 20_000;

/**
 * Odosle, co caka v rade, v poradi, v akom to vzniklo - poradie je tu podstatne: pridanie veci
 * musi prejst skor nez jej odskrtnutie.
 *
 * Vypadok siete rad necha stat a skusi sa neskor. Odpoved servera ho posunie dalej, aj ked je
 * odmietava: server uz povedal svoje a opakovanim by sa nic nezmenilo. Kolko takych bolo, si
 * appka spocita a povie to nahlas - zmena, ktora sa neulozila, sa nema stratit potichu.
 */
export async function flush(): Promise<void> {
	if (sync.sending) return;

	sync.sending = true;

	let sent = false;

	try {
		for (;;) {
			const waiting = await listWrites();

			sync.pending = waiting.length;

			if (waiting.length === 0) break;

			const write = waiting[0];

			try {
				await api(write.path, {
					method: write.method,
					body: write.body === undefined ? undefined : JSON.stringify(write.body)
				});

				sync.online = true;
			} catch (error) {
				if (!(error instanceof ApiError)) {
					// Siet je stale prec. Rad ostava tak, ako je, a skusi sa po navrate.
					sync.online = false;
					return;
				}

				// Server ma zlu chvilu; to nie je odpoved o nasom zapise, tak sa este zopakuje.
				if (error.status >= 500) return;

				// Zmazane uz zmazane je - to je presne to, co sme chceli. Ostatne odmietnutia
				// (nazov medzitym pribudol, polozku zmazal ten druhy) su strata a povie sa o nej.
				if (!(error.status === 404 && write.method === 'DELETE')) {
					sync.rejected += 1;
				}
			}

			await dropWrite(write.seq!);

			sent = true;
		}
	} finally {
		sync.sending = false;
	}

	// Po odoslani je pravda opat na serveri - obrazovky si ju nacitaju nanovo.
	if (sent) {
		await invalidateAll();
	}
}

/**
 * Pripoji appku na to, co sa deje so sietou. Rad sa odosiela pri navrate spojenia, pri navrate
 * k appke (na telefone sa medzitym uspava) a inak sam od seba, kym v nom nieco stoji.
 *
 * Vracia funkciu, ktora to vsetko zase odpoji - layout ju vola pri odchode.
 */
export function watchNetwork(): () => void {
	sync.online = navigator.onLine;

	const online = () => {
		sync.online = true;
		flush();
	};

	const offline = () => {
		sync.online = false;
	};

	const woken = () => {
		if (document.visibilityState === 'visible') {
			sync.online = navigator.onLine;
			flush();
		}
	};

	// Rad sa vola sam len vtedy, ked v nom nieco je - inak by sa appka budila pre nic.
	const timer = setInterval(() => {
		if (sync.pending > 0 && navigator.onLine) flush();
	}, RetryEvery);

	addEventListener('online', online);
	addEventListener('offline', offline);
	document.addEventListener('visibilitychange', woken);

	countWrites().then((count) => {
		sync.pending = count;

		if (count > 0) flush();
	});

	return () => {
		removeEventListener('online', online);
		removeEventListener('offline', offline);
		document.removeEventListener('visibilitychange', woken);
		clearInterval(timer);
	};
}
