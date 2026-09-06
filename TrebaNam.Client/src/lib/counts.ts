import { formatLocale } from '$lib/locale';
import { m } from '$lib/paraglide/messages.js';

/**
 * Vety s poctom. Anglictina rozlisuje jednu vec a ostatne, slovencina k tomu este dva az
 * styri ("2 veci", ale "5 veci"), takze o tvar sa neda rozhodovat podmienkou na obrazovke -
 * pyta sa Intl.PluralRules a kluc sa vyberie podla toho, co odpovie.
 *
 * Kazdy pocitany text ma preto svoju funkciu tu a nikde inde sa uz nesklada.
 */
function form(count: number): 'one' | 'few' | 'other' {
	const rule = new Intl.PluralRules(formatLocale()).select(count);

	return rule === 'one' || rule === 'few' ? rule : 'other';
}

export function listCount(count: number) {
	switch (form(count)) {
		case 'one':
			return m.list_count_one();
		case 'few':
			return m.list_count_few({ count });
		default:
			return m.list_count_other({ count });
	}
}

export function memberCount(count: number) {
	switch (form(count)) {
		case 'one':
			return m.household_member_count_one();
		case 'few':
			return m.household_member_count_few({ count });
		default:
			return m.household_member_count_other({ count });
	}
}

/** Kolko poloziek malo za sebou ukonceny nakup. */
export function tripItemCount(count: number) {
	switch (form(count)) {
		case 'one':
			return m.history_count_one();
		case 'few':
			return m.history_count_few({ count });
		default:
			return m.history_count_other({ count });
	}
}

/** Zvysok zoznamu pod nahladom na domovskej obrazovke. */
export function listMore(count: number) {
	switch (form(count)) {
		case 'one':
			return m.app_home_list_more_one();
		case 'few':
			return m.app_home_list_more_few({ count });
		default:
			return m.app_home_list_more_other({ count });
	}
}

/** Kolko zapisov caka na odoslanie, kym nie je signal. */
export function waitingCount(count: number) {
	switch (form(count)) {
		case 'one':
			return m.offline_waiting_one();
		case 'few':
			return m.offline_waiting_few({ count });
		default:
			return m.offline_waiting_other({ count });
	}
}

/** Kolko z nich server odmietol - o stratenu zmenu sa clovek ma dozvediet. */
export function rejectedCount(count: number) {
	switch (form(count)) {
		case 'one':
			return m.offline_rejected_one();
		case 'few':
			return m.offline_rejected_few({ count });
		default:
			return m.offline_rejected_other({ count });
	}
}
