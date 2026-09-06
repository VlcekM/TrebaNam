import { formatLocale } from '$lib/locale';

/**
 * Datum nakupu. Dlhy tvar je ten z handoffu, kratky je pre uzke obrazovky - na mobile stoja
 * vedla datumu suma aj kos a "Saturday, September 5" by sa zalomilo na tri riadky.
 *
 * Formatovace si drzime podla jazyka. V ramci jedneho nacitania sa jazyk nemeni - prepnutie
 * stranku nacita odznova - takze staci pamatat si posledny.
 */
let cached: { locale: string; long: Intl.DateTimeFormat; short: Intl.DateTimeFormat } | undefined;

function formats() {
	const locale = formatLocale();

	if (cached?.locale !== locale) {
		cached = {
			locale,
			long: new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long' }),
			short: new Intl.DateTimeFormat(locale, { weekday: 'short', day: 'numeric', month: 'short' })
		};
	}

	return cached;
}

export function tripDate(completedAt: string) {
	const date = new Date(completedAt);
	const { long, short } = formats();

	return { long: long.format(date), short: short.format(date) };
}
