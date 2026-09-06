import { getLocale, locales, setLocale } from '$lib/paraglide/runtime';

export type Locale = (typeof locales)[number];

/**
 * Jazyk sa vzdy pise vo svojom jazyku - "Slovensky" nie "Slovak". Kto hlada svoju rec
 * v menu, ju hlada tak, ako ju pozna, nie v preklade toho jazyka, ktory prave nerozumie.
 */
export const LOCALE_NAMES: Record<Locale, string> = {
	en: 'English',
	sk: 'Slovensky'
};

/**
 * Znacka pre Intl. Nie je to to iste ako jazyk appky: eurove sumy a datumy maju vyzerat
 * podla zvyklosti krajiny (24,90 € proti €24.90), aj ked retazce su len jedny.
 */
const FORMAT_LOCALES: Record<Locale, string> = {
	en: 'en-IE',
	sk: 'sk-SK'
};

export function formatLocale() {
	return FORMAT_LOCALES[getLocale()] ?? FORMAT_LOCALES.en;
}

/**
 * Dalsi jazyk v poradi. Su dva, takze prepinac je prepinac a nie ponuka; ked ich raz bude
 * viac, patri sem zoznam a nie dalsia podmienka na kazdom mieste, kde sa prepina.
 */
export function nextLocale(): Locale {
	const current = getLocale();

	return locales[(locales.indexOf(current) + 1) % locales.length];
}

/** Prepnutie jazyka stranku znova nacita, takze po nom je v jednom jazyku uplne vsetko. */
export function switchLocale(locale: Locale) {
	setLocale(locale);
}
