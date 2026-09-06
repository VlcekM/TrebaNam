/**
 * Datum nakupu. Dlhy tvar je ten z handoffu, kratky je pre uzke obrazovky - na mobile stoja
 * vedla datumu suma aj kos a "Saturday, September 5" by sa zalomilo na tri riadky.
 */
const long = new Intl.DateTimeFormat('en', { weekday: 'long', day: 'numeric', month: 'long' });

const short = new Intl.DateTimeFormat('en', { weekday: 'short', day: 'numeric', month: 'short' });

export function tripDate(completedAt: string) {
	const date = new Date(completedAt);

	return { long: long.format(date), short: short.format(date) };
}
