import { formatLocale } from '$lib/locale';
import type { Category, ShoppingRecord } from '$lib/types';
import { builtinLabel, categoryName } from '$lib/categories';

/**
 * Mesiac ako "2026-09" - kluc, podla ktoreho sa nakupy filtruju a scitavaju. Berie sa
 * miestny cas telefonu, nie UTC: nakup z prveho v noci patri do mesiaca, v ktorom sa
 * skutocne isiel, nie do toho, ktory bezi v Greenwichi.
 */
export type MonthKey = string;

export function monthOf(date: Date | string): MonthKey {
	const value = typeof date === 'string' ? new Date(date) : date;

	return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}`;
}

export function monthDate(month: MonthKey) {
	const [year, index] = month.split('-').map(Number);

	return new Date(year, index - 1, 1);
}

export function shiftMonth(month: MonthKey, by: number): MonthKey {
	const date = monthDate(month);

	return monthOf(new Date(date.getFullYear(), date.getMonth() + by, 1));
}

/** "september 2026" / "September 2026" - podla jazyka appky, rovnako ako datum nakupu. */
export function monthLabel(month: MonthKey) {
	return new Intl.DateTimeFormat(formatLocale(), { month: 'long', year: 'numeric' }).format(
		monthDate(month)
	);
}

/** Kratky tvar pod stlpec grafu: "sep", "Sep". */
export function monthShort(month: MonthKey) {
	return new Intl.DateTimeFormat(formatLocale(), { month: 'short' }).format(monthDate(month));
}

export function tripsInMonth(records: readonly ShoppingRecord[], month: MonthKey) {
	return records.filter((record) => monthOf(record.completedAt) === month);
}

/**
 * Sucet sum za nakupy. Nakup bez sumy do suctu nic nepridava, ale pocita sa zvlast, aby
 * obrazovka mohla povedat, ze cislo je bez neho - inak by nula vyzerala ako nic neminute.
 */
export function totals(records: readonly ShoppingRecord[]) {
	let spent = 0;
	let withoutTotal = 0;

	for (const record of records) {
		if (record.totalCost == null) withoutTotal++;
		else spent += record.totalCost;
	}

	return { spent, withoutTotal, trips: records.length };
}

export interface CategorySpend {
	code: string;
	label: string;
	amount: number;
	/** Podiel na sucte, 0 az 1. */
	share: number;
	/** Poradie skupiny v domacnosti - podla neho sa vybera farba, nie podla velkosti. */
	slot: number;
}

/**
 * Minutie po skupinach. Cena je len na celom nakupe, nie na riadku, takze sa suma nakupu
 * rozdeli rovnym dielom medzi jeho polozky a kazdy diel ide do skupiny svojej polozky. Je
 * to odhad - masla a paradajky nestoja rovnako - ale jediny, ktory sa z toho, co appka vie,
 * da urobit; obrazovka to hovori nahlas. Nakup bez sumy sem neprispieva vobec.
 *
 * Farba skupiny je dana poradim v domacnosti, nie poradim podla vysky, aby medziach mesiacmi
 * nepreskakovala. Skupina, ktoru domacnost medzitym zrusila, dostane miesto na konci.
 */
export function spendByCategory(
	records: readonly ShoppingRecord[],
	categories: readonly Category[]
): CategorySpend[] {
	const amounts = new Map<string, number>();

	for (const record of records) {
		if (record.totalCost == null || record.items.length === 0) continue;

		const share = record.totalCost / record.items.length;

		for (const item of record.items) {
			amounts.set(item.category, (amounts.get(item.category) ?? 0) + share);
		}
	}

	const slots = new Map(categories.map((category, index) => [category.code, index]));
	const labels = new Map(categories.map((category) => [category.code, categoryName(category)]));
	let next = categories.length;
	const sum = [...amounts.values()].reduce((a, b) => a + b, 0);

	return [...amounts.entries()]
		.map(([code, amount]) => {
			let slot = slots.get(code);

			if (slot === undefined) {
				slot = next++;
				slots.set(code, slot);
			}

			return {
				code,
				label: labels.get(code) ?? builtinLabel(code),
				amount,
				share: sum > 0 ? amount / sum : 0,
				slot
			};
		})
		.sort((a, b) => b.amount - a.amount);
}

/** Sucty pre okno mesiacov konciace danym mesiacom - podklad pre stlpcovy graf. */
export function monthlyTotals(records: readonly ShoppingRecord[], last: MonthKey, count: number) {
	const byMonth = new Map<MonthKey, number>();

	for (const record of records) {
		if (record.totalCost == null) continue;

		const key = monthOf(record.completedAt);
		byMonth.set(key, (byMonth.get(key) ?? 0) + record.totalCost);
	}

	const months: { month: MonthKey; spent: number }[] = [];

	for (let i = count - 1; i >= 0; i--) {
		const month = shiftMonth(last, -i);
		months.push({ month, spent: byMonth.get(month) ?? 0 });
	}

	return months;
}

/** Mesiace, v ktorych bol aspon jeden nakup, od najnovsieho. */
export function monthsWithTrips(records: readonly ShoppingRecord[]) {
	return [...new Set(records.map((record) => monthOf(record.completedAt)))].sort().reverse();
}

/** Najskorsi mesiac s nakupom - odtial zacina ponuka mesiacov. */
export function earliestMonth(records: readonly ShoppingRecord[]) {
	const months = monthsWithTrips(records);

	return months[months.length - 1];
}
