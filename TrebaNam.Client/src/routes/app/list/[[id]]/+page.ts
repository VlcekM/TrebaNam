import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { loadJson } from '$lib/offline/net';
import type { Item } from '$lib/types';

/**
 * Bez adresy zoznamu plati ten prvy, takze /app/list otvori to, s cim domacnost zacala, a
 * kazdy dalsi zoznam ma vlastnu adresu. Bez domacnosti nie je co kupovat - najprv ju treba
 * zalozit alebo prijat pozvanku.
 */
export const load: PageLoad = async ({ parent, fetch, params }) => {
	const { household, lists } = await parent();

	if (!household || lists.length === 0) {
		redirect(302, '/app/household');
	}

	const list = params.id ? lists.find((one) => one.id === params.id) : lists[0];

	// Zoznam mohol medzitym zmazat ten druhy; vtedy je namieste prvy, nie chybova stranka.
	if (!list) {
		redirect(302, '/app/list');
	}

	// Polozky vsetkych zoznamov tahame naraz a triedime tu: prehlad ich aj tak potrebuje vsetky
	// a jedno nacitanie je menej cakania nez jedno na kazdy zoznam.
	const items = (await loadJson<Item[]>('/api/items', fetch)) ?? [];

	return { list, items: items.filter((item) => item.listID === list.id) };
};
