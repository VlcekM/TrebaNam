/**
 * Sumy su v eurach - domacnost je jedna a plati jednou menou, takze si ju appka nikde nevybera.
 * Ked raz bude treba viac mien, patri to na domacnost, nie na jednotlivy nakup.
 */
const format = new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' });

export function money(amount: number) {
	return format.format(amount);
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
