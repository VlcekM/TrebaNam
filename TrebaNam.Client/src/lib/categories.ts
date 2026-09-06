import { m } from '$lib/paraglide/messages.js';
import type { Item, ShoppingRecordItem } from '$lib/types';

/** Rovnake kody ako ItemCategory.All v API a v rovnakom poradi - podla neho idu skupiny za sebou. */
export const CATEGORIES = ['produce', 'bakery', 'dairy', 'pantry', 'household', 'other'] as const;

export type Category = (typeof CATEGORIES)[number];

const LABELS: Record<Category, () => string> = {
	produce: m.category_produce,
	bakery: m.category_bakery,
	dairy: m.category_dairy,
	pantry: m.category_pantry,
	household: m.category_household,
	other: m.category_other
};

export function categoryLabel(code: string) {
	return (LABELS[code as Category] ?? m.category_other)();
}

/**
 * Zoskupi polozky po kategoriach v poradi z CATEGORIES a prazdne skupiny zahodi -
 * v obchode sa chodi po oddeleniach, nie po case pridania.
 */
export function byCategory<T extends Item | ShoppingRecordItem>(items: T[]) {
	return CATEGORIES.map((category) => ({
		category,
		items: items.filter((item) => item.category === category)
	})).filter((group) => group.items.length > 0);
}
