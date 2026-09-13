import { m } from '$lib/paraglide/messages.js';
import { formatLocale } from '$lib/locale';
import type { Category, Item, ShoppingRecordItem } from '$lib/types';

/** Kody, s ktorymi domacnost zacina - rovnake ako ItemCategory.Defaults v API. */
const BUILTIN: Record<string, () => string> = {
	produce: m.category_produce,
	bakery: m.category_bakery,
	dairy: m.category_dairy,
	pantry: m.category_pantry,
	household: m.category_household,
	other: m.category_other
};

/** Zakladna skupina sa cita v jazyku appky; neznamy kod je "ostatne", nie chyba. */
export function builtinLabel(code: string) {
	return (BUILTIN[code] ?? m.category_other)();
}

/**
 * Nazov skupiny. Co si domacnost pomenovala sama, sa cita tak, ako si to napisala - v oboch
 * jazykoch rovnako, lebo obchod je jeden a nazov oddelenia v nom tiez.
 */
export function categoryName(category: Category) {
	return category.name ?? builtinLabel(category.code);
}

/** Nazov ku kodu z polozky. Ked uz skupina neexistuje (niekto ju zrusil), plati zakladny nazov. */
export function categoryLabel(code: string, categories: readonly Category[] = []) {
	const found = categories.find((category) => category.code === code);

	return found ? categoryName(found) : builtinLabel(code);
}

/**
 * Zoskupi polozky po skupinach a prazdne skupiny zahodi - v obchode sa chodi po oddeleniach,
 * nie po case pridania. Poradie urcuje domacnost, lebo kazdy obchod ma oddelenia inak.
 *
 * Co ostane mimo (skupinu medzitym niekto zrusil) ide na koniec vo vlastnej skupine - zo
 * zoznamu nesmie polozka zmiznut len preto, ze sa zmenilo, ako sa veci triedia.
 */
export function byCategory<T extends Item | ShoppingRecordItem>(
	items: T[],
	categories: readonly Category[] = []
) {
	const known = new Set(categories.map((category) => category.code));
	// V skupine podla abecedy, nie podla pridania: na regali sa hlada ocami po nazve.
	const collator = new Intl.Collator(formatLocale());
	const of = (code: string) =>
		items.filter((item) => item.category === code).sort((a, b) => collator.compare(a.name, b.name));

	const groups = categories.map((category) => ({
		code: category.code,
		label: categoryName(category),
		items: of(category.code)
	}));

	for (const item of items) {
		if (known.has(item.category)) continue;

		known.add(item.category);
		groups.push({
			code: item.category,
			label: builtinLabel(item.category),
			items: of(item.category)
		});
	}

	return groups.filter((group) => group.items.length > 0);
}

/**
 * Skupiny podla abecedy v jazyku appky - zakladne sa porovnavaju podla prekladu, lebo tak ich
 * clovek cita. "Ostatne" ostava posledne: je to dno, na ktore pada vsetko bez skupiny, a na
 * zozname aj v obchode patri az za vsetko, co ma vlastne oddelenie.
 */
export function alphabetical(categories: readonly Category[]) {
	const collator = new Intl.Collator(formatLocale());

	return [...categories].sort((a, b) => {
		if (a.code === 'other' || b.code === 'other') return a.code === 'other' ? 1 : -1;

		return collator.compare(categoryName(a), categoryName(b));
	});
}

/**
 * Kam novu skupinu zaradit: pred prvu, ktora je podla abecedy za nou. Server ju dava na
 * koniec, lebo nazvy zakladnych skupin pozna len klient; v abecednom zozname tak skonci tam,
 * kam patri, a v inak zoradenom aspon nie na konci za "ostatne".
 */
export function insertAlphabetically(categories: readonly Category[], added: Category) {
	const collator = new Intl.Collator(formatLocale());
	const rest = categories.filter((category) => category.id !== added.id);
	const name = categoryName(added);

	let index = rest.findIndex(
		(category) => category.code === 'other' || collator.compare(categoryName(category), name) > 0
	);

	if (index === -1) index = rest.length;

	return [...rest.slice(0, index), added, ...rest.slice(index)];
}
