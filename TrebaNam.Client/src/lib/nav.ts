import { m } from '$lib/paraglide/messages.js';

/** Navigacia appky. Jedna definicia pre bocny panel aj mobilnu hlavicku, nech sa nerozidu. */
export function navItems() {
	return [
		{ href: '/app', label: m.app_nav_home() },
		{ href: '/app/list', label: m.app_nav_list() },
		{ href: '/app/history', label: m.app_nav_history() },
		{ href: '/app/household', label: m.app_nav_household() }
	];
}

/**
 * /app je domovska obrazovka, takze sa zvyrazni len na presnej zhode; ostatne polozky
 * aj na podstranach. Koncova lomka moze a nemusi byt, podla toho odkial navigacia prisla.
 */
export function isActive(pathname: string, href: string) {
	const path = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname;

	// Domovska obrazovka nema podstranky, inak by svietila uplne vsade.
	if (href === '/app') {
		return path === '/app';
	}

	// Rezim nakupu je podobrazovka zoznamu, nie vlastna polozka - navigacia ostava na zozname.
	if (href === '/app/list') {
		return path === '/app/list' || path === '/app/shop';
	}

	return path === href || path.startsWith(`${href}/`);
}
