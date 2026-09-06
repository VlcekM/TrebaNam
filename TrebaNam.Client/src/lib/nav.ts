import { m } from '$lib/paraglide/messages.js';

/** Navigacia appky. Jedna definicia pre bocny panel aj mobilnu hlavicku, nech sa nerozidu. */
export function navItems() {
	return [
		{ href: '/app', label: m.app_nav_buy() },
		{ href: '/app/household', label: m.app_nav_household() }
	];
}

/**
 * /app je domovska obrazovka, takze sa zvyrazni len na presnej zhode; ostatne polozky
 * aj na podstranach. Koncova lomka moze a nemusi byt, podla toho odkial navigacia prisla.
 */
export function isActive(pathname: string, href: string) {
	const path = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname;

	return href === '/app' ? path === '/app' : path === href || path.startsWith(`${href}/`);
}
