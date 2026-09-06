import { invalidateAll } from '$app/navigation';

/** Rovnaka cesta a rovnake meno spravy ako v HouseholdChannel na serveri. */
const HubPath = '/api/hub/household';

const Changed = 'changed';

/**
 * Spojenie, cez ktore sa domacnost dozvie o zmene skor, nez si niekto obnovi stranku. Cez hub
 * nechodia data, len sprava "pozri sa na to znova" - obrazovky sa aj tak plnia zo svojich
 * loadov, takze realny cas nemoze ukazat nieco ine nez bezne nacitanie.
 *
 * Ked spojenie nevznikne alebo spadne, appka funguje presne ako predtym: zmeny sa objavia
 * pri najblizsom nacitani. Nic sa preto na hub necaka a zlyhanie sa nikomu nehlasi.
 */
export function connectHousehold() {
	let connection: { stop(): Promise<void> } | undefined;
	let stopped = false;

	// Obe strany zvyknu zapisovat naraz (jeden odskrtava, druhy pridava), takze sprav chodi
	// viac za sebou; nacitanie spustame raz a pripadny dalsi podnet si odlozime na koniec.
	let refreshing = false;
	let again = false;

	async function refresh() {
		if (refreshing) {
			again = true;
			return;
		}

		refreshing = true;

		try {
			do {
				again = false;
				await invalidateAll();
			} while (again);
		} finally {
			refreshing = false;
		}
	}

	// Kniznicu tahame az tu: bez domacnosti a mimo prehliadaca nie je co pocuvat.
	(async () => {
		const signalR = await import('@microsoft/signalr');

		const hub = new signalR.HubConnectionBuilder()
			.withUrl(HubPath)
			.withAutomaticReconnect()
			.build();

		hub.on(Changed, refresh);

		// Kym bolo spojenie prec, mohlo sa nieco zmenit - po navrate teda nacitame vsetko.
		hub.onreconnected(refresh);

		try {
			await hub.start();
		} catch {
			return;
		}

		if (stopped) {
			await hub.stop();
			return;
		}

		connection = hub;
	})();

	return () => {
		stopped = true;
		connection?.stop();
	};
}
