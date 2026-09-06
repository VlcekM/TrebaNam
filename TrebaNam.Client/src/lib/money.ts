import { formatLocale } from '$lib/locale';

/**
 * Sumy su v eurach - domacnost je jedna a plati jednou menou, takze si ju appka nikde nevybera.
 * Ked raz bude treba viac mien, patri to na domacnost, nie na jednotlivy nakup. Zapis sa riadi
 * jazykom appky, takze po slovensky je to "24,90 €" a po anglicky "€24.90".
 */
let cached: { locale: string; format: Intl.NumberFormat } | undefined;

export function money(amount: number) {
	const locale = formatLocale();

	if (cached?.locale !== locale) {
		cached = {
			locale,
			format: new Intl.NumberFormat(locale, { style: 'currency', currency: 'EUR' })
		};
	}

	return cached.format.format(amount);
}

/**
 * Cislo z formulara. Berieme aj desatinnu ciarku, lebo na slovenskej klavesnici je to
 * to, co clovek napise; nezmysel vratime ako undefined a formular sa oznami sam.
 */
export function parseMoney(text: string): number | undefined {
	const cleaned = text.replace(/[\s€]/g, '').replace(',', '.');

	if (!cleaned) return undefined;

	const value = Number(cleaned);

	return Number.isFinite(value) && value >= 0 ? value : undefined;
}
